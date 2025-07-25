using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightJson.Serialization
{
	using ErrorType = JsonParseException.ErrorType;

	/// <summary>
	/// Represents a reader that provides fast, forward-only access to JSON data in a text-based format.
	/// </summary>
	public sealed class JsonReader : IDisposable
	{
		private readonly TextScanner scanner;
		private string moutingPath = "$";
		private readonly JsonOptions options;
		private bool disposedValue;
		private bool leaveOpen = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonReader"/> class.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader"/> that contains the JSON data to read.</param>
		/// <param name="options">The <see cref="JsonOptions"/> that specify the behavior of the <see cref="JsonReader"/>.</param>
		/// <param name="leaveOpen">A value that indicates whether the underlying <paramref name="reader"/> should remain open after the <see cref="JsonReader"/> object is disposed. Defaults to <c>false</c>.</param>
		public JsonReader(TextReader reader, JsonOptions options, bool leaveOpen = false)
		{
			this.scanner = new TextScanner(reader);
			this.options = options;
			this.leaveOpen = leaveOpen;
		}

		/// <summary>
		/// Creates an new instance of the <see cref="JsonReader"/> class with given parameters.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader"/> stream where the JSON input is.</param>
		public JsonReader(TextReader reader) : this(reader, JsonOptions.Default)
		{
		}

		private string ReadJsonKey(CancellationToken cancellation)
		{
			string read;
			if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowUnquotedPropertyNames))
			{
				read = this.ReadUnquotedProperty(cancellation);
			}
			else
			{
				read = this.ReadString(cancellation);
			}
			this.moutingPath += "." + read;
			return read;
		}

		private JsonValue ReadJsonValue(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			this.scanner.SkipWhitespace(cancellation);
			this.SkipComments(cancellation);

			var next = this.scanner.Peek();

			if (char.IsNumber(next))
			{
				return this.ReadNumber(cancellation);
			}

			switch (next)
			{
				case '{':
					return new JsonValue(this.ReadObject(cancellation), this.options);

				case '[':
					return new JsonValue(this.ReadArray(cancellation), this.options);

				case '"':
				case '\'':
					return new JsonValue(this.ReadString(cancellation), this.options);

				case '-':
				case '.':
				case '+':
					return this.ReadNumber(cancellation);

				case 't':
				case 'f':
					return this.ReadBoolean();

				case 'n':
					return this.ReadNull();

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
					return new JsonValue(true, this.options);

				case 'f':
					this.scanner.Assert("false");
					return new JsonValue(false, this.options);

				default:
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
			}
		}

		private void ReadDigits(StringBuilder builder, CancellationToken cancellation)
		{
			while (this.scanner.CanRead && !TextScanner.IsNumericValueTerminator(this.scanner.PeekOrDefault()))
			{
				cancellation.ThrowIfCancellationRequested();
				_ = builder.Append(this.scanner.Read());
			}
		}

		private JsonValue ReadNumber(CancellationToken cancellation)
		{
			var builder = new StringBuilder();
			var peek = this.scanner.Peek();

			if (peek == '-')
			{
				_ = builder.Append(this.scanner.Read());
				this.ReadDigits(builder, cancellation);
			}
			else if (peek == '.')
			{
				_ = builder.Append("0");
			}
			else if (peek == '+')
			{
				if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowPositiveSign))
				{
					_ = builder.Append(this.scanner.Read());
					this.ReadDigits(builder, cancellation);
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
				this.ReadDigits(builder, cancellation);
			}

			if (this.scanner.CanRead && this.scanner.Peek() == '.')
			{
				_ = builder.Append(this.scanner.Read());

				var n = this.scanner.PeekOrDefault();

				if (char.IsDigit(n))
				{
					this.ReadDigits(builder, cancellation);
				}
				else
				{
					if (TextScanner.IsNumericValueTerminator(n))
					{
						if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.TrailingDecimalPoint))
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
				_ = builder.Append(this.scanner.Read());

				var next = this.scanner.Peek();

				switch (next)
				{
					case '+':
					case '-':
						_ = builder.Append(this.scanner.Read());
						break;
				}

				this.ReadDigits(builder, cancellation);
			}

			string s = builder.ToString();

			if (s.Length > 1 && s[0] == '0' && s[1] == 'x' && this.options.SerializationFlags.HasFlag(JsonSerializationFlags.HexadecimalNumberLiterals))
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

		private string ReadUnquotedProperty(CancellationToken cancellation)
		{
			var builder = new StringBuilder();

			var peek = this.scanner.Peek();
			if (peek == '"' || peek == '\'')
			{
				// it's an quoted string
				return this.ReadString(cancellation);
			}

			int l = 0;
			while (true)
			{
				cancellation.ThrowIfCancellationRequested();
				var c = this.scanner.Read();

				if (this.scanner.Peek() == ':')
				{
					_ = builder.Append(c);
					break;
				}

				// IdentifierName
				if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '$')
				{
					_ = builder.Append(c);
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

		private string ReadString(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			var builder = new StringBuilder();

			bool isSingleQuoted = false;
			char h = this.scanner.AssertAny(['"', '\'']);

			bool isParsingMultilineLiteral = false;
			int quoteCount = 1;

			_ = this.scanner.Read();

			if (h == '\'')
			{
				isSingleQuoted = true;
				if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowSingleQuotes))
				{
					throw new JsonParseException(
						ErrorType.InvalidOrUnexpectedCharacter,
						this.scanner.Position
					);
				}
			}

			while (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.MultilineStringLiterals))
			{
				var current = this.scanner.Peek();
				if (current == h)
				{
					char c = this.scanner.Read();
					builder.Append(c);
					quoteCount++;
				}
				else if (current == '\n' || current == '\r')
				{
					isParsingMultilineLiteral = quoteCount > 1;
					builder.Clear();
					break;
				}
				else
				{
					builder.Clear();
					break;
				}
			}

			if (isParsingMultilineLiteral)
			{
				int currentIncidenceCount = 0;
				while (true)
				{
					cancellation.ThrowIfCancellationRequested();
					var c = this.scanner.Read();

					if (c == h)
					{
						currentIncidenceCount++;
						if (currentIncidenceCount == quoteCount)
						{
							StringBuilder finalResult = new StringBuilder();
							int startLineCount = -1;
							foreach (var line in builder.ToString().Split('\n'))
							{
								if (startLineCount == -1 && line.Length > 0)
								{
									startLineCount = line.Length - line.TrimStart().Length;
								}

								if (line.Length >= startLineCount && startLineCount >= 0)
								{
									finalResult.AppendLine(line[startLineCount..]);
								}
								else
								{
									finalResult.AppendLine(line);
								}
							}

							return finalResult.ToString().Trim();
						}
					}
					else
					{
						if (currentIncidenceCount > 0)
						{
							builder.Append(new string(h, currentIncidenceCount));
							currentIncidenceCount = 0;
						}
						builder.Append(c);
					}
				}
			}
			else
			{
				bool usedNlLiteral = false;
				while (true)
				{
					cancellation.ThrowIfCancellationRequested();
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
								_ = builder.Append(c);
								break;
							case 'b':
								_ = builder.Append('\b');
								break;
							case 'f':
								_ = builder.Append('\f');
								break;
							case 'n':
								_ = builder.Append('\n');
								break;
							case 'r':
								_ = builder.Append('\r');
								break;
							case 't':
								_ = builder.Append('\t');
								break;
							case 'u':
								_ = builder.Append(this.ReadUnicodeLiteral());
								break;
							case '\n' or '\r':
								if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
								{
									usedNlLiteral = true;
									_ = builder.Append("\\\n");
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
							if ((c is '\n' or '\r') && this.options.SerializationFlags.HasFlag(JsonSerializationFlags.AllowStringLineBreaks))
							{
								usedNlLiteral = true;
								_ = builder.Append(c);
							}
							else
								throw new JsonParseException(
								ErrorType.InvalidOrUnexpectedCharacter,
								this.scanner.Position
							);
						}
						else
						{
							_ = builder.Append(c);
						}
					}
				}

				if (usedNlLiteral)
				{
					string[] copyLines = builder.ToString().Split('\n');
					_ = builder.Clear();

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
							_ = builder.Append(line[new Range(0, Index.FromEnd(1))]);
						}
						else if (i == copyLines.Length - 1)
						{
							_ = builder.Append(line);
						}
						else
						{
							_ = builder.AppendLine(line);
						}
					}
				}

				return builder.ToString();
			}
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

			value += this.ReadHexDigit() * 4096; // 16^3
			value += this.ReadHexDigit() * 256;  // 16^2
			value += this.ReadHexDigit() * 16;   // 16^1
			value += this.ReadHexDigit();        // 16^0

			return (char)value;
		}

		private JsonObject ReadObject(CancellationToken cancellation)
		{
			return this.ReadObject(new JsonObject(this.options), cancellation);
		}

		private JsonObject ReadObject(JsonObject jsonObject, CancellationToken cancellation)
		{
			string initialPath = this.moutingPath;

			cancellation.ThrowIfCancellationRequested();

			this.scanner.Assert('{');

			this.scanner.SkipWhitespace(cancellation);
			this.SkipComments(cancellation);

			if (this.scanner.Peek() == '}')
			{
				_ = this.scanner.Read();
			}
			else
			{
				while (true)
				{
					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);
					cancellation.ThrowIfCancellationRequested();

					if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma) && this.scanner.Peek() == '}')
					{
						_ = this.scanner.Read();
						break;
					}

					var key = this.ReadJsonKey(cancellation);

					// https://www.rfc-editor.org/rfc/rfc7159#section-4
					// JSON should allow duplicate key names by default
					if (this.options.ThrowOnDuplicateObjectKeys)
					{
						if (jsonObject.ContainsKey(key))
						{
							throw new JsonParseException(
								ErrorType.DuplicateObjectKeys,
								this.scanner.Position
							);
						}
					}

					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);

					this.scanner.Assert(':');

					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);

					var value = this.ReadJsonValue(cancellation);

					value.path = this.moutingPath;

					jsonObject[key] = value;

					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);

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
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
				}
			}

			jsonObject.path = this.moutingPath;
			return jsonObject;
		}

		private JsonArray ReadArray(CancellationToken cancellation)
		{
			return this.ReadArray(new JsonArray(this.options), cancellation);
		}

		private JsonArray ReadArray(JsonArray jsonArray, CancellationToken cancellation)
		{
			string initialPath = this.moutingPath;
			int index = 0;

			cancellation.ThrowIfCancellationRequested();

			this.scanner.Assert('[');

			this.scanner.SkipWhitespace(cancellation);
			this.SkipComments(cancellation);

			if (this.scanner.Peek() == ']')
			{
				_ = this.scanner.Read();
			}
			else
			{
				while (true)
				{
					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);
					cancellation.ThrowIfCancellationRequested();

					if (this.scanner.Peek() == ']')
					{
						if (this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreTrailingComma))
						{
							_ = this.scanner.Read();
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

					this.moutingPath += $"[{index}]";
					var value = this.ReadJsonValue(cancellation);

					value.path = this.moutingPath;

					jsonArray.Add(value);

					this.scanner.SkipWhitespace(cancellation);
					this.SkipComments(cancellation);

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
						throw new JsonParseException(
							ErrorType.InvalidOrUnexpectedCharacter,
							this.scanner.Position
						);
					}
				}
			}

			jsonArray.path = this.moutingPath;
			return jsonArray;
		}

		private void SkipComments(CancellationToken cancellation)
		{
			if (!this.options.SerializationFlags.HasFlag(JsonSerializationFlags.IgnoreComments))
			{
				return;
			}
		checkNextComment:
			this.scanner.SkipWhitespace(cancellation);
			if (this.scanner.Peek() == '/')
			{
				_ = this.scanner.Read();
				bool isMultilineComment = this.scanner.Peek() == '*';

				while (this.scanner.CanRead)
				{
					cancellation.ThrowIfCancellationRequested();
					char c = this.scanner.Read();

					if (isMultilineComment)
					{
						if (c == '*' && this.scanner.Peek() == '/')
						{
							_ = this.scanner.Read();
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
				return;
			}
		}

		/// <summary>
		/// Parses the JSON value from the input.
		/// </summary>
		/// <returns>A <see cref="JsonValue"/> representing the parsed JSON.</returns>
		public JsonValue Parse()
		{
			this.scanner.SkipWhitespace(default);
			return this.ReadJsonValue(default);
		}

		/// <summary>
		/// Asynchronously parses the JSON value from the input.
		/// </summary>
		/// <param name="cancellation">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
		/// <returns>A <see cref="Task"/> that returns a <see cref="JsonValue"/> representing the parsed JSON.</returns>
		public Task<JsonValue> ParseAsync(CancellationToken cancellation = default)
		{
			return Task.Run(delegate
			{
				this.scanner.SkipWhitespace(cancellation);
				return this.ReadJsonValue(cancellation);
			}, cancellation);
		}

		/// <summary>
		/// Attempts to read the inner <see cref="TextReader"/> into an valid JSON and returns an boolean indicating the
		/// result.
		/// </summary>
		/// <param name="result">When this method returns, it outputs an <see cref="JsonValue"/> with the result of the operation.</param>
		/// <returns>An boolean indicating if the input could be read into a valid JSON or not.</returns>
		public bool TryParse(out JsonValue result)
		{
			try
			{
				result = this.Parse();
				return true;
			}
			catch
			{
				result = default;
				return false;
			}
		}

		/// <summary>
		/// Asynchronously attempts to read the inner <see cref="TextReader"/> into a valid JSON and returns a tuple containing
		/// a boolean indicating the result and the parsed <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="cancellation">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
		/// <returns>A tuple containing a boolean indicating if the input could be read into a valid JSON or not, and the
		/// parsed <see cref="JsonValue"/>.</returns>
		public async Task<JsonDeserializationResult> TryParseAsync(CancellationToken cancellation = default)
		{
			JsonValue result = default;
			try
			{
				result = await this.ParseAsync(cancellation);
				return new(null, result);
			}
			catch (JsonParseException epx)
			{
				return new($"Line: {epx.Position.Line}, Column: {epx.Position.Column} | {epx.Type}: {epx.Message}", result);
			}
			catch (Exception ex)
			{
				return new(ex.Message, result);
			}
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					if (!this.leaveOpen)
						this.scanner.Dispose();
				}

				this.disposedValue = true;
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
