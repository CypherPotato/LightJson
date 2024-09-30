﻿using System;
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
		private readonly TextScanner scanner;
		private string moutingPath = "$";
		private readonly JsonOptions options;
		internal bool canThrowExceptions = true;
		internal bool caughtException = false;

		internal JsonReader(TextReader reader, JsonOptions options)
		{
			this.scanner = new TextScanner(reader);
			this.options = options;
		}

		JsonValue ThrowParseException(Exception ex) => this.ThrowParseException<JsonValue>(ex);

		T ThrowParseException<T>(Exception ex)
		{
			if (this.canThrowExceptions)
			{
				throw ex;
			}
			else
			{
				this.caughtException = true;
				return default(T)!;
			}
		}

		private string ReadJsonKey()
		{
			string read;
			if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowUnquotedPropertyNames))
			{
				read = this.ReadUnquotedProperty();
			}
			else
			{
				read = this.ReadString();
			}
			this.moutingPath += "." + read;
			return read;
		}

		private JsonValue ReadJsonValue()
		{
			this.scanner.SkipWhitespace();

			this.SkipComments();

			var next = this.scanner.Peek();
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonObject>(this.scanner.Exception);

			if (char.IsNumber(next))
			{
				return this.ReadNumber();
			}

			switch (next)
			{
				case '{':
					return new JsonValue(this.ReadObject(), this.options);

				case '[':
					return new JsonValue(this.ReadArray(), this.options);

				case '"':
				case '\'':
					return new JsonValue(this.ReadString(), this.options);

				case '-':
				case '.':
				case '+':
					return this.ReadNumber();

				case 't':
				case 'f':
					return this.ReadBoolean();

				case 'n':
					return this.ReadNull();

				default:
					return this.ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
			}
		}

		private JsonValue ReadNull()
		{
			this.scanner.Assert("null");
			if (this.scanner.Exception is not null)
				return this.ThrowParseException(this.scanner.Exception);
			return JsonValue.Null;
		}

		private JsonValue ReadBoolean()
		{
			switch (this.scanner.Peek())
			{
				case 't':
					this.scanner.Assert("true");
					if (this.scanner.Exception is not null)
						return this.ThrowParseException(this.scanner.Exception);

					return new JsonValue(true, this.options);

				case 'f':
					this.scanner.Assert("false");
					if (this.scanner.Exception is not null)
						return this.ThrowParseException(this.scanner.Exception);

					return new JsonValue(false, this.options);

				default:
					if (this.scanner.Exception is not null)
						return this.ThrowParseException<JsonObject>(this.scanner.Exception);

					return this.ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
			}
		}

		private void ReadDigits(StringBuilder builder)
		{
			while (this.scanner.CanRead && !TextScanner.IsNumericValueTerminator(this.scanner.PeekOrDefault()))
			{
				builder.Append(this.scanner.Read());
			}
			if (this.scanner.Exception is not null)
				this.ThrowParseException(this.scanner.Exception);
		}

		private JsonValue ReadNumber()
		{
			var builder = new StringBuilder();
			var peek = this.scanner.Peek();
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonObject>(this.scanner.Exception);

			if (peek == '-')
			{
				builder.Append(this.scanner.Read());
				this.ReadDigits(builder);
			}
			else if (peek == '.')
			{
				builder.Append("0");
			}
			else if (peek == '+')
			{
				if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowPositiveSign))
				{
					builder.Append(this.scanner.Read());
					this.ReadDigits(builder);
				}
				else
				{
					return this.ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
				}
			}
			else
			{
				this.ReadDigits(builder);
			}

			if (this.scanner.CanRead && this.scanner.Peek() == '.')
			{
				builder.Append(this.scanner.Read());

				var n = this.scanner.PeekOrDefault();

				if (char.IsDigit(n))
				{
					this.ReadDigits(builder);
				}
				else
				{
					if (TextScanner.IsNumericValueTerminator(n))
					{
						if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.TrailingDecimalPoint))
						{
							return this.ThrowParseException(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							));
						}
					}
					else
					{
						return this.ThrowParseException(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						));
					}
				}
			}
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonObject>(this.scanner.Exception);

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

				this.ReadDigits(builder);
			}

			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonObject>(this.scanner.Exception);

			string s = builder.ToString();

			if (s.Length > 1 && s[0] == '0' && s[1] == 'x' && this.options.SerializationFlags.HasFlag(JsonSerializationFlags.HexadecimalNumberLiterals))
			{
				// hex literal
				string hex = s[2..];

				if (hex == "")
				{
					return this.ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
				}

				var num = Convert.ToInt32(hex, 16);
				return new JsonValue((double)num, this.options);
			}
			else
			{
				if (s.IndexOf('_') >= 1 && this.options.SerializationFlags.HasFlag(JsonSerializationFlags.NumericUnderscoreLiterals))
				{
					s = s.Replace("_", "");
				}

				return new JsonValue(double.Parse(s, CultureInfo.InvariantCulture), this.options);
			}
		}

		private string ReadUnquotedProperty()
		{
			var builder = new StringBuilder();

			var peek = this.scanner.Peek();
			if (peek == '"' || peek == '\'')
			{
				// it's an quoted string
				return this.ReadString();
			}

			if (this.scanner.Exception is not null)
				this.ThrowParseException(this.scanner.Exception);

			int l = 0;
			while (true)
			{
				var c = this.scanner.Read();

				if (this.scanner.Peek() == ':')
				{
					builder.Append(c);
					break;
				}

				if (this.scanner.Exception is not null)
					this.ThrowParseException(this.scanner.Exception);

				// IdentifierName

				if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '$')
				{
					builder.Append(c);
				}
				else
				{
					return this.ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
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
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<string>(this.scanner.Exception);

			this.scanner.Read();

			if (h == '\'')
			{
				isSingleQuoted = true;
				if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowSingleQuotes))
				{
					return this.ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
				}
			}

			int lineStartColumn = (int)this.scanner.Position.column;
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
							builder.Append(this.ReadUnicodeLiteral());
							break;
						case '\n' or '\r':
							if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
							{
								usedNlLiteral = true;
								builder.Append("\\\n");
							}
							else
							{
								return this.ThrowParseException<string>(new JsonParseException(
									ErrorType.InvalidOrUnexpectedCharacter,
									this.scanner.Position
								));
							}
							break;

						default:
							return this.ThrowParseException<string>(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							));
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
						if ((c is '\n' or '\r') && this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
						{
							usedNlLiteral = true;
							builder.Append(c);
						}
						else return this.ThrowParseException<string>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						));
					}
					else
					{
						builder.Append(c);
					}
				}
			}

			if (usedNlLiteral)
			{
				string[] copyLines = builder.ToString().Split('\n');
				builder.Clear();

				bool nextIsContinuation = false;
				for (int i = 0; i < copyLines.Length; i++)
				{
					string line = copyLines[i].TrimEnd();

					if (nextIsContinuation)
					{
						line = line.TrimStart();
						nextIsContinuation = false;
					}

					if (line.EndsWith('\\'))
					{
						nextIsContinuation = true;
						builder.Append(line[new Range(0, Index.FromEnd(1))]);
					}
					else if (i == copyLines.Length - 1)
					{
						builder.Append(line);
					}
					else
					{
						builder.AppendLine(line);
					}
				}
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
					return this.ThrowParseException<int>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					));
			}
		}

		private char ReadUnicodeLiteral()
		{
			int value = 0;

			value += this.ReadHexDigit() * 4096; // 16^3
			value += this.ReadHexDigit() * 256;  // 16^2
			value += this.ReadHexDigit() * 16;   // 16^1
			value += this.ReadHexDigit();        // 16^0

			return (char)value;
		}

		private JsonObject ReadObject()
		{
			return this.ReadObject(new JsonObject(this.options));
		}

		private JsonObject ReadObject(JsonObject jsonObject)
		{
			string initialPath = this.moutingPath;

			this.scanner.Assert('{');
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonObject>(this.scanner.Exception);

			this.scanner.SkipWhitespace();
			this.SkipComments();

			if (this.scanner.Peek() == '}')
			{
				this.scanner.Read();
			}
			else
			{
				if (this.scanner.Exception is not null)
					return this.ThrowParseException<JsonObject>(this.scanner.Exception);

				while (true)
				{
					this.scanner.SkipWhitespace();
					this.SkipComments();

					if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma) && this.scanner.Peek() == '}')
					{
						this.scanner.Read();
						break;
					}

					if (this.scanner.Exception is not null)
						return this.ThrowParseException<JsonObject>(this.scanner.Exception);

					var key = this.ReadJsonKey();

					// https://www.rfc-editor.org/rfc/rfc7159#section-4
					// JSON should allow duplicate key names by default
					if (this.options.ThrowOnDuplicateObjectKeys)
					{
						if (jsonObject.ContainsKey(key))
						{
							return this.ThrowParseException<JsonObject>(new JsonParseException(
								ErrorType.DuplicateObjectKeys,
								this.scanner.Position
							));
						}
					}

					this.scanner.SkipWhitespace();
					this.SkipComments();

					this.scanner.Assert(':');
					if (this.scanner.Exception is not null)
						return this.ThrowParseException<JsonObject>(this.scanner.Exception);

					this.scanner.SkipWhitespace();
					this.SkipComments();

					var value = this.ReadJsonValue();

					value.path = this.moutingPath;

					jsonObject[key] = value;

					this.scanner.SkipWhitespace();
					this.SkipComments();

					var next = this.scanner.Read();
					this.moutingPath = initialPath;

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
						return this.ThrowParseException<JsonObject>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						));
					}
				}
			}

			jsonObject.path = this.moutingPath;
			return jsonObject;
		}

		private JsonArray ReadArray()
		{
			return this.ReadArray(new JsonArray(this.options));
		}

		private JsonArray ReadArray(JsonArray jsonArray)
		{
			string initialPath = this.moutingPath;
			int index = 0;

			this.scanner.Assert('[');
			if (this.scanner.Exception is not null)
				return this.ThrowParseException<JsonArray>(this.scanner.Exception);

			this.scanner.SkipWhitespace();
			this.SkipComments();

			if (this.scanner.Peek() == ']')
			{
				this.scanner.Read();
			}
			else
			{
				if (this.scanner.Exception is not null)
					return this.ThrowParseException<JsonArray>(this.scanner.Exception);

				while (true)
				{
					this.scanner.SkipWhitespace();
					this.SkipComments();

					if (this.scanner.Peek() == ']')
					{
						if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma))
						{
							this.scanner.Read();
							break;
						}
						else
						{
							return this.ThrowParseException<JsonArray>(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							));
						}
					}

					if (this.scanner.Exception is not null)
						return this.ThrowParseException<JsonArray>(this.scanner.Exception);

					this.moutingPath += $"[{index}]";
					var value = this.ReadJsonValue();

					value.path = this.moutingPath;

					jsonArray.Add(value);

					this.scanner.SkipWhitespace();
					this.SkipComments();

					this.moutingPath = initialPath;
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
						return this.ThrowParseException<JsonArray>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						));
					}
				}
			}

			jsonArray.path = this.moutingPath;
			return jsonArray;
		}

		private void SkipComments()
		{
			if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreComments))
			{
				return;
			}
		checkNextComment:
			this.scanner.SkipWhitespace();
			if (this.scanner.Peek() == '/')
			{
				this.scanner.Read();
				bool isMultilineComment = this.scanner.Peek() == '*';

				while (this.scanner.CanRead)
				{
					char c = this.scanner.Read();

					if (isMultilineComment)
					{
						if (c == '*' && this.scanner.Peek() == '/')
						{
							this.scanner.Read();
							break;
						}
					}
					else
					{
						if (c is '\n' or '\r')
						{
							break;
						}
					}
				}

				goto checkNextComment;
			}
			else
			{
				if (this.scanner.Exception is not null)
					this.ThrowParseException(this.scanner.Exception);
				return;
			}
		}

		internal JsonValue Parse()
		{
			this.scanner.SkipWhitespace();
			return this.ReadJsonValue();
		}

		/// <summary>
		/// Tries to create a JsonValue by using the given TextReader.
		/// </summary>
		/// <param name="reader">The TextReader used to read a JSON message.</param>
		/// <param name="options">Specifies the JsonOptions used to read values.</param>
		/// <param name="result">The <see cref="JsonValue"/> resulting the operating.</param>
		public static bool TryParse(TextReader reader, JsonOptions? options, out JsonValue result)
		{
			if (reader is null)
			{
				throw new ArgumentNullException("reader");
			}

			var jreader = new JsonReader(reader, options ?? JsonOptions.Default);
			jreader.canThrowExceptions = false;
			jreader.scanner.CanThrowExceptions = false;

			result = jreader.Parse();
			return !jreader.caughtException;
		}

		/// <summary>
		/// Tries to create a JsonValue by using the given string.
		/// </summary>
		/// <param name="text">The TextReader used to read a JSON message.</param>
		/// <param name="options">Specifies the JsonOptions used to read values.</param>
		/// <param name="result">The <see cref="JsonValue"/> resulting the operating.</param>
		public static bool TryParse(string text, JsonOptions? options, out JsonValue result)
		{
			using (var reader = new StringReader(text))
			{
				return TryParse(reader, options, out result);
			}
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
			//if (opt.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreComments))
			//	source = JsonSanitizer.SanitizeInput(source);

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
