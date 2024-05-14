using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LightJson.Serialization
{
	using ErrorType = JsonParseException.ErrorType;

	/// <summary>
	/// Represents a reader that can read JsonValues.
	/// </summary>
	public sealed class JsonReader
	{
		private TextScanner scanner;
		private string moutingPath = "$";
		private JsonOptions options;

		private JsonReader(TextReader reader, JsonOptions options)
		{
			this.scanner = new TextScanner(reader);
			this.options = options;
		}

		private string ReadJsonKey()
		{
			string read;
			if (options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowUnquotedPropertyNames))
			{
				read = ReadUnquotedProperty();
			}
			else
			{
				read = ReadString();
			}
			moutingPath += "." + read;
			return read;
		}

		private JsonValue ReadJsonValue()
		{
			this.scanner.SkipWhitespace();

			var next = this.scanner.Peek();

			if (char.IsNumber(next))
			{
				return ReadNumber();
			}

			switch (next)
			{
				case '{':
					return new JsonValue(ReadObject(), options);

				case '[':
					return new JsonValue(ReadArray(), options);

				case '"':
				case '\'':
					return new JsonValue(ReadString(), options);

				case '-':
				case '.':
				case '+':
					return ReadNumber();

				case 't':
				case 'f':
					return ReadBoolean();

				case 'n':
					return ReadNull();

				default:
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
			}
		}

		private JsonValue ReadNull()
		{
			this.scanner.Assert("null");
			return JsonValue.Null;
		}

		private JsonValue ReadBoolean()
		{
			switch (this.scanner.Peek())
			{
				case 't':
					this.scanner.Assert("true");
					return new JsonValue(true, options);

				case 'f':
					this.scanner.Assert("false");
					return new JsonValue(false, options);

				default:
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
			}
		}

		private void ReadDigits(StringBuilder builder)
		{
			while (this.scanner.CanRead && !TextScanner.IsNumericValueTerminator(this.scanner.PeekOrDefault()))
			{
				builder.Append(this.scanner.Read());
			}
		}

		private JsonValue ReadNumber()
		{
			var builder = new StringBuilder();
			var peek = this.scanner.Peek();

			if (peek == '-')
			{
				builder.Append(this.scanner.Read());
				ReadDigits(builder);
			}
			else if (peek == '.')
			{
				builder.Append("0");
			}
			else if (peek == '+')
			{
				if (options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowPositiveSign))
				{
					builder.Append(this.scanner.Read());
					ReadDigits(builder);
				}
				else
				{
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
				}
			}
			else
			{
				ReadDigits(builder);
			}

			if (this.scanner.CanRead && this.scanner.Peek() == '.')
			{
				builder.Append(this.scanner.Read());

				var n = this.scanner.PeekOrDefault();

				if (char.IsDigit(n))
				{
					ReadDigits(builder);
				}
				else
				{
					if (TextScanner.IsNumericValueTerminator(n))
					{
						if (!options.SerializationFlags.HasFlag(JsonSerializationFlags.TrailingDecimalPoint))
						{
							throw new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							);
						}
					}
					else
					{
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
				}
			}

			if (this.scanner.CanRead && char.ToLowerInvariant(this.scanner.Peek()) == 'e')
			{
				builder.Append(this.scanner.Read());

				var next = this.scanner.Peek();

				switch (next)
				{
					case '+':
					case '-':
						builder.Append(this.scanner.Read());
						break;
				}

				ReadDigits(builder);
			}

			string s = builder.ToString();

			if (s.Length > 1 && s[0] == '0' && s[1] == 'x' && options.SerializationFlags.HasFlag(JsonSerializationFlags.HexadecimalNumberLiterals))
			{
				// hex literal
				string hex = s[2..];

				if (hex == "")
				{
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
				}

				var num = Convert.ToInt32(hex, 16);
				return new JsonValue((double)num, options);
			}
			else
			{
				if (s.IndexOf('_') >= 1 && options.SerializationFlags.HasFlag(JsonSerializationFlags.NumericUnderscoreLiterals))
				{
					s = s.Replace("_", "");
				}

				return new JsonValue(double.Parse(s, CultureInfo.InvariantCulture), options);
			}
		}

		private string ReadUnquotedProperty()
		{
			var builder = new StringBuilder();

			var peek = scanner.Peek();
			if (peek == '"' || peek == '\'')
			{
				// it's an quoted string
				return ReadString();
			}

			int l = 0;
			while (true)
			{
				var c = this.scanner.Read();

				if (scanner.Peek() == ':')
				{
					builder.Append(c);
					break;
				}

				// IdentifierName

				if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '$')
				{
					builder.Append(c);
				}
				else
				{
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
				}

				l++;
			}

			return builder.ToString();
		}

		private string ReadString()
		{
			var builder = new StringBuilder();

			bool isSingleQuoted = false;
			char h = this.scanner.AssertAny('"', '\'');
			this.scanner.Read();

			if (h == '\'')
			{
				isSingleQuoted = true;
				if (!options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowSingleQuotes))
				{
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
				}
			}

			long lineStartColumn = scanner.Position.column;
			bool usedNlLiteral = false;
			while (true)
			{
				var c = this.scanner.Read();

				if (c == '\\')
				{
					c = this.scanner.Read();

					switch (char.ToLower(c))
					{
						case '"':  // "
						case '\'': // '
						case '\\': // \
						case '/':  // /
							builder.Append(c);
							break;
						case 'b':
							builder.Append('\b');
							break;
						case 'f':
							builder.Append('\f');
							break;
						case 'n':
							builder.Append('\n');
							break;
						case 'r':
							builder.Append('\r');
							break;
						case 't':
							builder.Append('\t');
							break;
						case 'u':
							builder.Append(ReadUnicodeLiteral());
							break;
						case '\n' or '\r':
							if (options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
							{
								usedNlLiteral = true;
								builder.Append("\\\n");
							}
							else
							{
								throw new JsonParseException(
									ErrorType.InvalidOrUnexpectedCharacter,
									this.scanner.Position
								);
							}
							break;

						default:
							throw new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							);
					}
				}
				else if (!isSingleQuoted && c == '"')
				{
					break;
				}
				else if (isSingleQuoted && c == '\'')
				{
					break;
				}
				else
				{
					// According to the spec:
					//
					// unescaped = %x20-21 / %x23-5B / %x5D-10FFFF
					//
					// i.e. c cannot be < 0x20, be 0x22 (a double quote) or a
					// backslash (0x5C).
					//
					// c cannot be a back slash or double quote as the above
					// would have hit. So just check for < 0x20.
					//
					// > 0x10FFFF is unnecessary *I think* because it's obviously
					// out of the range of a character but we might need to look ahead
					// to get the whole utf-16 codepoint.
					if (c < '\u0020')
					{
						if ((c == '\n' || c == '\r') && options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
						{
							usedNlLiteral = true;
							builder.Append(c);
						}
						else throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
					else
					{
						builder.Append(c);
					}
				}
			}

			if (usedNlLiteral && options.SerializationFlags.HasFlag(JsonSerializationFlags.NormalizeStringBreakSpace))
			{
				string[] copyLines = builder.ToString().Split('\n');
				builder.Clear();

				bool nextIsContinuation = false;
				for (int i = 0; i < copyLines.Length; i++)
				{
					string line = copyLines[i];
					bool isTerminator = line[Index.FromEnd(1)] == '\\';

					if (isTerminator)
					{
						line = line[new Range(0, Index.FromEnd(1))];
						;
					}

					if (line.Length > lineStartColumn)
					{
						int j = 0;
						while (j < lineStartColumn && char.IsWhiteSpace(line[j]))
							j++;

						if (nextIsContinuation || isTerminator)
						{
							builder.Append(line[j..]);
						}
						else
						{
							builder.AppendLine(line[j..]);
						}

						if (nextIsContinuation)
						{
							nextIsContinuation = false;
						}
					}
					else
					{
						builder.AppendLine(line);
					}

					if (isTerminator)
					{
						nextIsContinuation = true;
						;
					}
				}
				;
			}

			return builder.ToString();
		}

		private int ReadHexDigit()
		{
			switch (char.ToUpper(this.scanner.Read()))
			{
				case '0':
					return 0;

				case '1':
					return 1;

				case '2':
					return 2;

				case '3':
					return 3;

				case '4':
					return 4;

				case '5':
					return 5;

				case '6':
					return 6;

				case '7':
					return 7;

				case '8':
					return 8;

				case '9':
					return 9;

				case 'A':
					return 10;

				case 'B':
					return 11;

				case 'C':
					return 12;

				case 'D':
					return 13;

				case 'E':
					return 14;

				case 'F':
					return 15;

				default:
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
			}
		}

		private char ReadUnicodeLiteral()
		{
			int value = 0;

			value += ReadHexDigit() * 4096; // 16^3
			value += ReadHexDigit() * 256;  // 16^2
			value += ReadHexDigit() * 16;   // 16^1
			value += ReadHexDigit();        // 16^0

			return (char)value;
		}

		private JsonObject ReadObject()
		{
			return ReadObject(new JsonObject(options));
		}

		private JsonObject ReadObject(JsonObject jsonObject)
		{
			string initialPath = moutingPath;

			this.scanner.Assert('{');

			this.scanner.SkipWhitespace();

			if (this.scanner.Peek() == '}')
			{
				this.scanner.Read();
			}
			else
			{
				while (true)
				{
					this.scanner.SkipWhitespace();

					if (options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma) && scanner.Peek() == '}')
					{
						this.scanner.Read();
						break;
					}

					var key = ReadJsonKey();

					// https://www.rfc-editor.org/rfc/rfc7159#section-4
					// JSON should allow duplicate key names by default
					if (options.ThrowOnDuplicateObjectKeys)
					{
						if (jsonObject.ContainsKey(key))
						{
							throw new JsonParseException(
								ErrorType.DuplicateObjectKeys,
								this.scanner.Position
							);
						}
					}

					this.scanner.SkipWhitespace();

					this.scanner.Assert(':');

					this.scanner.SkipWhitespace();

					var value = ReadJsonValue();

					value.path = moutingPath;

					jsonObject[key] = value;

					this.scanner.SkipWhitespace();

					var next = this.scanner.Read();
					moutingPath = initialPath;

					if (next == '}')
					{
						break;
					}
					else if (next == ',')
					{
						continue;
					}
					else
					{
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
				}
			}

			jsonObject.path = moutingPath;
			return jsonObject;
		}

		private JsonArray ReadArray()
		{
			return ReadArray(new JsonArray(options));
		}

		private JsonArray ReadArray(JsonArray jsonArray)
		{
			string initialPath = moutingPath;
			int index = 0;

			this.scanner.Assert('[');

			this.scanner.SkipWhitespace();

			if (this.scanner.Peek() == ']')
			{
				this.scanner.Read();
			}
			else
			{
				while (true)
				{
					this.scanner.SkipWhitespace();

					if (scanner.Peek() == ']')
					{
						if (options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma))
						{
							scanner.Read();
							break;
						}
						else
						{
							throw new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							);
						}
					}

					moutingPath += $"[{index}]";
					var value = ReadJsonValue();

					value.path = moutingPath;

					jsonArray.Add(value);

					this.scanner.SkipWhitespace();

					moutingPath = initialPath;
					var next = this.scanner.Read();

					if (next == ']')
					{
						break;
					}
					else if (next == ',')
					{
						index++;
						continue;
					}
					else
					{
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
				}
			}

			jsonArray.path = moutingPath;
			return jsonArray;
		}

		private JsonValue Parse()
		{
			this.scanner.SkipWhitespace();
			return ReadJsonValue();
		}

		/// <summary>
		/// Creates a JsonValue by using the given TextReader.
		/// </summary>
		/// <param name="reader">The TextReader used to read a JSON message.</param>
		/// <param name="options">Optional. Specifies the JsonOptions used to read values.</param>
		public static JsonValue Parse(TextReader reader, JsonOptions? options = null)
		{
			if (reader is null)
			{
				throw new ArgumentNullException("reader");
			}

			return new JsonReader(reader, options ?? JsonOptions.Default).Parse();
		}

		/// <summary>
		/// Creates a JsonValue by reader the JSON message in the given string.
		/// </summary>
		/// <param name="source">The string containing the JSON message.</param>
		/// <param name="options">Optional. Specifies the JsonOptions used to read values.</param>
		public static JsonValue Parse(string source, JsonOptions? options = null)
		{
			if (source is null)
			{
				throw new ArgumentNullException("source");
			}

			var opt = options ?? JsonOptions.Default;
			if (opt.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreComments))
				source = JsonSanitizer.SanitizeInput(source);

			using (var reader = new StringReader(source))
			{
				return new JsonReader(reader, opt).Parse();
			}
		}

		/// <summary>
		/// Creates a JsonValue by reading the given file.
		/// </summary>
		/// <param name="path">The file path to be read.</param>
		/// <param name="options">Optional. Specifies the JsonOptions used to read values.</param>
		public static JsonValue ParseFile(string path, JsonOptions? options = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException("path");
			}

			// NOTE: FileAccess.Read is needed to be able to open read-only files
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
			using (var reader = new StreamReader(stream))
			{
				return new JsonReader(reader, options ?? JsonOptions.Default).Parse();
			}
		}
	}
}
