using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LightJson.Serialization
{
	using ErrorType = JsonSerializationException.ErrorType;

	/// <summary>
	/// Represents a TextWriter adapter that can write string representations of JsonValues.
	/// </summary>
	public sealed class JsonWriter : IDisposable
	{
		private int indent;
		private bool isNewLine;

		/// <summary>
		/// A set of containing all the collection objects (JsonObject/JsonArray) being rendered.
		/// It is used to prevent circular references; since collections that contain themselves
		/// will never finish rendering.
		/// </summary>
		private readonly HashSet<IEnumerable<JsonValue>> renderingCollections;

		/// <summary>
		/// Gets or sets the string representing a indent in the output.
		/// </summary>
		public string? IndentString { get; set; }

		/// <summary>
		/// Gets or sets the string representing a space in the output.
		/// </summary>
		public string? SpacingString { get; set; }

		/// <summary>
		/// Gets or sets the string representing a new line on the output.
		/// </summary>
		public string? NewLineString { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether JsonObject properties should be written in a deterministic order.
		/// </summary>
		public bool SortObjects { get; set; }

		/// <summary>
		/// Gets or sets the TextWriter to which this JsonWriter writes.
		/// </summary>
		public TextWriter InnerWriter { get; }

		/// <summary>
		/// Gets or sets an boolean indicating whether this <see cref="JsonWriter"/> should
		/// write unquoted property keys or not.
		/// </summary>
		public bool UnquotedPropertyKeys { get; set; } = false;

		/// <summary>
		/// Gets or sets the <see cref="JsonNamingPolicy"/> policy for this <see cref="JsonWriter"/>.
		/// </summary>
		public JsonNamingPolicy? NamingPolicy { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="TextEncoder"/> used to encode string literals.
		/// </summary>
		public TextEncoder? StringEncoder { get; set; }

		/// <summary>
		/// Gets or sets the output for the JSON writer when writing double Infinity numbers.
		/// </summary>
		public JsonInfinityHandleOption InfinityHandleOption { get; set; } = JsonInfinityHandleOption.WriteNull;

		/// <summary>
		/// Initializes an new instance of <see cref="JsonWriter"/>.
		/// </summary>
		public JsonWriter()
		{
			this.InnerWriter = new StringWriter();
			this.renderingCollections = [];
		}

		/// <summary>
		/// Initializes a new instance of JsonWriter.
		/// </summary>
		/// <param name="innerWriter">The TextWriter used to write JsonValues.</param>
		public JsonWriter(TextWriter innerWriter) : this(innerWriter, JsonOptions.Default) { }

		/// <summary>
		/// Initializes a new instance of JsonWriter.
		/// </summary>
		/// <param name="innerWriter">The TextWriter used to write JsonValues.</param>
		/// <param name="options">The JsonOptions used to write data.</param>
		public JsonWriter(TextWriter innerWriter, JsonOptions options)
		{
			if (options.WriteIndented)
			{
				this.UseIndentedSyntax();
			}

			this.NamingPolicy = options.NamingPolicy;
			this.InfinityHandleOption = options.InfinityHandler;
			this.renderingCollections = [];
			this.InnerWriter = innerWriter;
			this.StringEncoder = options.Encoder;
		}

		private void Write(string? text)
		{
			if (this.isNewLine)
			{
				this.isNewLine = false;
				this.WriteIndentation();
			}

			this.InnerWriter.Write(text);
		}

		private void WriteJsonKey(string key)
		{
			if (this.UnquotedPropertyKeys)
			{
				this.Write(key);
			}
			else
			{
				this.WriteEncodedString(key);
			}
		}

		private void WriteEncodedString(string text)
		{
			this.Write("\"");

			if (this.StringEncoder is not null)
			{
				this.InnerWriter.Write(this.StringEncoder.Encode(text));
			}
			else
			{
				for (int i = 0; i < text.Length; i += 1)
				{
					var currentChar = text[i];

					switch (currentChar)
					{
						case '\\':
							this.InnerWriter.Write("\\\\");
							break;

						case '\"':
							this.InnerWriter.Write("\\\"");
							break;

						case '/':
							this.InnerWriter.Write("\\/");
							break;

						case '\b':
							this.InnerWriter.Write("\\b");
							break;

						case '\f':
							this.InnerWriter.Write("\\f");
							break;

						case '\n':
							this.InnerWriter.Write("\\n");
							break;

						case '\r':
							this.InnerWriter.Write("\\r");
							break;

						case '\t':
							this.InnerWriter.Write("\\t");
							break;

						default:
							this.InnerWriter.Write(currentChar);
							break;
					}
				}
			}

			this.Write("\"");
		}

		private void WriteIndentation()
		{
			for (var i = 0; i < this.indent; i += 1)
			{
				this.Write(this.IndentString);
			}
		}

		private void WriteSpacing()
		{
			this.Write(this.SpacingString);
		}

		private void WriteLine()
		{
			this.Write(this.NewLineString);
			this.isNewLine = true;
		}

		private void WriteLine(string line)
		{
			this.Write(line);
			this.WriteLine();
		}

		private void AddRenderingCollection(IEnumerable<JsonValue> value)
		{
			if (!this.renderingCollections.Add(value))
			{
				throw new JsonSerializationException(ErrorType.CircularReference);
			}
		}

		private void RemoveRenderingCollection(IEnumerable<JsonValue> value)
		{
			_ = this.renderingCollections.Remove(value);
		}

		private void Render(JsonValue value)
		{
			switch (value.Type)
			{
				case JsonValueType.Null:
					this.Write("null");
					break;

				case JsonValueType.Boolean:
					this.Write(value.GetBoolean() ? "true" : "false");
					break;

				case JsonValueType.Number:
					var number = value.GetNumber();
					if (double.IsPositiveInfinity(number) || double.IsNegativeInfinity(number))
					{
						if (this.InfinityHandleOption == JsonInfinityHandleOption.WriteZero)
						{
							this.Write("0");
						}
						else
						{
							this.Write("null");
						}
					}
					else
					{
						this.Write(number.ToString(CultureInfo.InvariantCulture));
					}
					break;

				case JsonValueType.String:
					this.WriteEncodedString(value.GetString());
					break;

				case JsonValueType.Object:
					this.Render(value.GetJsonObject());
					break;

				case JsonValueType.Array:
					this.Render(value.GetJsonArray());
					break;

				case JsonValueType.Undefined:
					throw new JsonSerializationException(ErrorType.RenderUndefined);

				default:
					throw new JsonSerializationException(ErrorType.InvalidValueType);
			}
		}

		private void Render(JsonArray value)
		{
			this.AddRenderingCollection(value);
			this.WriteLine("[");
			this.indent += 1;

			using (var enumerator = value.GetEnumerator())
			{
				var hasNext = enumerator.MoveNext();
				while (hasNext)
				{
					this.Render(enumerator.Current);
					hasNext = enumerator.MoveNext();

					if (hasNext)
					{
						this.WriteLine(",");
					}
					else
					{
						this.WriteLine();
					}
				}
			}

			this.indent -= 1;
			this.Write("]");
			this.RemoveRenderingCollection(value);
		}

		private void Render(JsonObject value)
		{
			this.AddRenderingCollection(value);
			this.WriteLine("{");
			this.indent += 1;

			using (var enumerator = this.GetJsonObjectEnumerator(value))
			{
				var hasNext = enumerator.MoveNext();
				while (hasNext)
				{
					string key = enumerator.Current.Key;
					key = this.NamingPolicy?.ConvertName(key) ?? key;

					this.WriteJsonKey(key);
					this.Write(":");
					this.WriteSpacing();
					this.Render(enumerator.Current.Value);

					hasNext = enumerator.MoveNext();

					if (hasNext)
					{
						this.WriteLine(",");
					}
					else
					{
						this.WriteLine();
					}
				}
			}

			this.indent -= 1;
			this.Write("}");
			this.RemoveRenderingCollection(value);
		}

		private IEnumerator<KeyValuePair<string, JsonValue>> GetJsonObjectEnumerator(JsonObject jsonObject)
		{
			if (this.SortObjects)
			{
				return jsonObject
					.GetProperties()
					.OrderBy(o => o.Key, StringComparer.Ordinal)
					.GetEnumerator();
			}
			else
			{
				return jsonObject.GetEnumerator();
			}
		}

		/// <summary>
		/// Sets this <see cref="JsonWriter"/> to write using an pretty, indented JSON syntax.
		/// </summary>
		public void UseIndentedSyntax()
		{
			this.IndentString = new string(' ', 4);
			this.SpacingString = " ";
			this.NewLineString = "\n";
		}

		/// <summary>
		/// Writes the given value to the writer.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to write.</param>
		public void Write(JsonValue jsonValue)
		{
			this.indent = 0;
			this.isNewLine = true;

			this.Render(jsonValue);

			this.renderingCollections.Clear();
		}

		/// <summary>
		/// Writes the specified JSON comment into the writer.
		/// </summary>
		/// <param name="comment">The comment text.</param>
		/// <param name="multiLine">Optional. Specifies if the comment should be written using an multi-line syntax or inline syntax.</param>
		/// <param name="padTopLine">Optional. Specifies if the writer should write and empty line before the comment.</param>
		public void WriteComment(string comment, bool multiLine = false, bool padTopLine = true)
		{
			if (padTopLine)
				this.InnerWriter.WriteLine();

			string[] lines = comment.Split('\n');

			if (multiLine)
			{
				this.InnerWriter.WriteLine("/*");
				for (int i = 0; i < lines.Length; i++)
				{
					this.InnerWriter.WriteLine(this.IndentString + lines[i].TrimEnd());
				}
				this.InnerWriter.WriteLine("*/");
			}
			else
			{
				for (int i = 0; i < lines.Length; i++)
				{
					this.InnerWriter.WriteLine("// " + lines[i].TrimEnd());
				}
			}
		}

		/// <summary>
		/// Serializes a <see cref="JsonValue"/> into a JSON string using the default <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="value">The <see cref="JsonValue"/> to serialize.</param>
		/// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
		public static string Serialize(JsonValue value)
		{
			return Serialize(value, JsonOptions.Default);
		}

		/// <summary>
		/// Serializes a <see cref="JsonValue"/> into a JSON string using the specified <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="value">The <see cref="JsonValue"/> to serialize.</param>
		/// <param name="options">The <see cref="JsonOptions"/> to use for serialization.</param>
		/// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
		public static string Serialize(JsonValue value, JsonOptions options)
		{
			using (var sw = new StringWriter())
			using (var jsonWriter = new JsonWriter(sw, options))
			{
				jsonWriter.Write(value);
				return sw.ToString();
			}
		}

		/// <summary>
		/// Serializes a <see cref="JsonValue"/> into a JSON string with optional formatting options.
		/// </summary>
		/// <param name="value">The <see cref="JsonValue"/> to serialize.</param>
		/// <param name="prettyOutput">If <c>true</c>, the JSON output will be formatted with indentation.</param>
		/// <param name="unquotedPropertyKeys">If <c>true</c>, property keys will not be quoted in the JSON output.</param>
		/// <param name="namingPolicy">The <see cref="JsonNamingPolicy"/> to use for serialization. If <c>null</c>, the default naming policy will be used.</param>
		/// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
		public static string Serialize(JsonValue value, bool prettyOutput = false, bool unquotedPropertyKeys = false, JsonNamingPolicy? namingPolicy = null)
		{
			using (var sw = new StringWriter())
			using (var jsonWriter = new JsonWriter(sw))
			{
				if (prettyOutput)
					jsonWriter.UseIndentedSyntax();

				jsonWriter.NamingPolicy = namingPolicy;
				jsonWriter.UnquotedPropertyKeys = unquotedPropertyKeys;

				jsonWriter.Write(value);
				return sw.ToString();
			}
		}

		/// <summary>
		/// Returns a string containing the characters written to the current <see cref="JsonWriter"/> so far.
		/// </summary>
		public override string ToString()
		{
			return this.InnerWriter.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.InnerWriter?.Dispose();
		}
	}
}
