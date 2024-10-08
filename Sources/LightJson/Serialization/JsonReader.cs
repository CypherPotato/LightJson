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
			scanner = new TextScanner(reader);
			this.options = options;
		}

		JsonValue ThrowParseException(Exception ex) => ThrowParseException<JsonValue>(ex);

		T ThrowParseException<T>(Exception ex)
		{
			if (canThrowExceptions)
			{
				throw ex;
			}
			else
			{
				caughtException = true;
				return default(T)!;
			}
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
			scanner.SkipWhitespace();

			SkipComments();

			var next = scanner.Peek();
			if (scanner.Exception is not null)
				return ThrowParseException<JsonObject>(scanner.Exception);

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
					return ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
			}
		}

		private JsonValue ReadNull()
		{
			scanner.Assert("null");
			if (scanner.Exception is not null)
				return ThrowParseException(scanner.Exception);
			return JsonValue.Null;
		}

		private JsonValue ReadBoolean()
		{
			switch (scanner.Peek())
			{
				case 't':
					scanner.Assert("true");
					if (scanner.Exception is not null)
						return ThrowParseException(scanner.Exception);

					return new JsonValue(true, options);

				case 'f':
					scanner.Assert("false");
					if (scanner.Exception is not null)
						return ThrowParseException(scanner.Exception);

					return new JsonValue(false, options);

				default:
					if (scanner.Exception is not null)
						return ThrowParseException<JsonObject>(scanner.Exception);

					return ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
			}
		}

		private void ReadDigits(StringBuilder builder)
		{
			while (scanner.CanRead && !TextScanner.IsNumericValueTerminator(scanner.PeekOrDefault()))
			{
				builder.Append(scanner.Read());
			}
			if (scanner.Exception is not null)
				ThrowParseException(scanner.Exception);
		}

		private JsonValue ReadNumber()
		{
			var builder = new StringBuilder();
			var peek = scanner.Peek();
			if (scanner.Exception is not null)
				return ThrowParseException<JsonObject>(scanner.Exception);

			if (peek == '-')
			{
				builder.Append(scanner.Read());
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
					builder.Append(scanner.Read());
					ReadDigits(builder);
				}
				else
				{
					return ThrowParseException(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
				}
			}
			else
			{
				ReadDigits(builder);
			}

			if (scanner.CanRead && scanner.Peek() == '.')
			{
				builder.Append(scanner.Read());

				var n = scanner.PeekOrDefault();

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
							return ThrowParseException(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								scanner.Position
							));
						}
					}
					else
					{
						return ThrowParseException(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							scanner.Position
						));
					}
				}
			}
			if (scanner.Exception is not null)
				return ThrowParseException<JsonObject>(scanner.Exception);

			if (scanner.CanRead && char.ToLowerInvariant(scanner.Peek()) == 'e')
			{
				builder.Append(scanner.Read());

				var next = scanner.Peek();

				switch (next)
				{
					case '+':
					case '-':
						builder.Append(scanner.Read());
						break;
				}

				ReadDigits(builder);
			}

			if (scanner.Exception is not null)
				return ThrowParseException<JsonObject>(scanner.Exception);

			string s = builder.ToString();

			if (s.Length > 1 && s[0] == '0' && s[1] == 'x' && options.SerializationFlags.HasFlag(JsonSerializationFlags.HexadecimalNumberLiterals))
			{
				// hex literal
				string hex = s[2..];

				if (hex == "")
				{
					return ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
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

			if (scanner.Exception is not null)
				ThrowParseException(scanner.Exception);

			int l = 0;
			while (true)
			{
				var c = scanner.Read();

				if (scanner.Peek() == ':')
				{
					builder.Append(c);
					break;
				}

				if (scanner.Exception is not null)
					ThrowParseException(scanner.Exception);

				// IdentifierName

				if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '$')
				{
					builder.Append(c);
				}
				else
				{
					return ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
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
			char h = scanner.AssertAny('"', '\'');
			if (scanner.Exception is not null)
				return ThrowParseException<string>(scanner.Exception);

			scanner.Read();

			if (h == '\'')
			{
				isSingleQuoted = true;
				if (!options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowSingleQuotes))
				{
					return ThrowParseException<string>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
				}
			}

			int lineStartColumn = (int)scanner.Position.column;
			bool usedNlLiteral = false;
			while (true)
			{
				var c = scanner.Read();

				if (c == '\\')
				{
					c = scanner.Read();

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
								return ThrowParseException<string>(new JsonParseException(
									ErrorType.InvalidOrUnexpectedCharacter,
									scanner.Position
								));
							}
							break;

						default:
							return ThrowParseException<string>(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								scanner.Position
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
						if ((c is '\n' or '\r') && options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
						{
							usedNlLiteral = true;
							builder.Append(c);
						}
						else return ThrowParseException<string>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							scanner.Position
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
			switch (char.ToUpper(scanner.Read()))
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
					return ThrowParseException<int>(new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						scanner.Position
					));
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

			scanner.Assert('{');
			if (scanner.Exception is not null)
				return ThrowParseException<JsonObject>(scanner.Exception);

			scanner.SkipWhitespace();
			SkipComments();

			if (scanner.Peek() == '}')
			{
				scanner.Read();
			}
			else
			{
				if (scanner.Exception is not null)
					return ThrowParseException<JsonObject>(scanner.Exception);

				while (true)
				{
					scanner.SkipWhitespace();
					SkipComments();

					if (options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma) && scanner.Peek() == '}')
					{
						scanner.Read();
						break;
					}

					if (scanner.Exception is not null)
						return ThrowParseException<JsonObject>(scanner.Exception);

					var key = ReadJsonKey();

					// https://www.rfc-editor.org/rfc/rfc7159#section-4
					// JSON should allow duplicate key names by default
					if (options.ThrowOnDuplicateObjectKeys)
					{
						if (jsonObject.ContainsKey(key))
						{
							return ThrowParseException<JsonObject>(new JsonParseException(
								ErrorType.DuplicateObjectKeys,
								scanner.Position
							));
						}
					}

					scanner.SkipWhitespace();
					SkipComments();

					scanner.Assert(':');
					if (scanner.Exception is not null)
						return ThrowParseException<JsonObject>(scanner.Exception);

					scanner.SkipWhitespace();
					SkipComments();

					var value = ReadJsonValue();

					value.path = moutingPath;

					jsonObject[key] = value;

					scanner.SkipWhitespace();
					SkipComments();

					var next = scanner.Read();
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
						return ThrowParseException<JsonObject>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							scanner.Position
						));
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

			scanner.Assert('[');
			if (scanner.Exception is not null)
				return ThrowParseException<JsonArray>(scanner.Exception);

			scanner.SkipWhitespace();
			SkipComments();

			if (scanner.Peek() == ']')
			{
				scanner.Read();
			}
			else
			{
				if (scanner.Exception is not null)
					return ThrowParseException<JsonArray>(scanner.Exception);

				while (true)
				{
					scanner.SkipWhitespace();
					SkipComments();

					if (scanner.Peek() == ']')
					{
						if (options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma))
						{
							scanner.Read();
							break;
						}
						else
						{
							return ThrowParseException<JsonArray>(new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								scanner.Position
							));
						}
					}

					if (scanner.Exception is not null)
						return ThrowParseException<JsonArray>(scanner.Exception);

					moutingPath += $"[{index}]";
					var value = ReadJsonValue();

					value.path = moutingPath;

					jsonArray.Add(value);

					scanner.SkipWhitespace();
					SkipComments();

					moutingPath = initialPath;
					var next = scanner.Read();

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
						return ThrowParseException<JsonArray>(new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							scanner.Position
						));
					}
				}
			}

			jsonArray.path = moutingPath;
			return jsonArray;
		}

		private void SkipComments()
		{
			if (!options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreComments))
			{
				return;
			}
		checkNextComment:
			scanner.SkipWhitespace();
			if (scanner.Peek() == '/')
			{
				scanner.Read();
				bool isMultilineComment = scanner.Peek() == '*';

				while (scanner.CanRead)
				{
					char c = scanner.Read();

					if (isMultilineComment)
					{
						if (c == '*' && scanner.Peek() == '/')
						{
							scanner.Read();
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
				if (scanner.Exception is not null)
					ThrowParseException(scanner.Exception);
				return;
			}
		}

		internal JsonValue Parse()
		{
			scanner.SkipWhitespace();
			return ReadJsonValue();
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
