using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		/// Initializes an new instance of <see cref="JsonWriter"/>.
		/// </summary>
		public JsonWriter()
		{
			this.InnerWriter = new StringWriter();
			this.renderingCollections = new HashSet<IEnumerable<JsonValue>>();
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

			this.renderingCollections = new HashSet<IEnumerable<JsonValue>>();
			this.InnerWriter = innerWriter;
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

		private void WriteEncodedJsonValue(JsonValue value)
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
					this.Write(value.GetNumber().ToString(CultureInfo.InvariantCulture));
					break;

				case JsonValueType.String:
					this.WriteEncodedString(value.GetString());
					break;

				case JsonValueType.Object:
					this.Write(string.Format("JsonObject[{0}]", value.GetJsonObject().Count));
					break;

				case JsonValueType.Array:
					this.Write(string.Format("JsonArray[{0}]", value.GetJsonArray().Count));
					break;

				default:
					throw new InvalidOperationException("Invalid value type.");
			}
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

			for (int i = 0; i < text.Length; i += 1)
			{
				var currentChar = text[i];

				// Encoding special characters.
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

			this.InnerWriter.Write("\"");
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
			this.renderingCollections.Remove(value);
		}

		private void Render(JsonValue value)
		{
			switch (value.Type)
			{
				case JsonValueType.Null:
				case JsonValueType.Boolean:
				case JsonValueType.Number:
				case JsonValueType.String:
					this.WriteEncodedJsonValue(value);
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

		/// <summary>
		/// Gets an JsonObject enumarator based on the configuration of this JsonWriter.
		/// If JsonWriter.SortObjects is set to true, then a ordered enumerator is returned.
		/// Otherwise, a faster non-deterministic enumerator is returned.
		/// </summary>
		/// <param name="jsonObject">The JsonObject for which to get an enumerator.</param>
		private IEnumerator<KeyValuePair<string, JsonValue>> GetJsonObjectEnumerator(JsonObject jsonObject)
		{
			if (this.SortObjects)
			{
				var sortedDictionary = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);

				foreach (var item in jsonObject)
				{
					sortedDictionary.Add(item.Key, item.Value);
				}

				return sortedDictionary.GetEnumerator();
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
					this.InnerWriter.WriteLine(this.IndentString + lines[i]);
				}
				this.InnerWriter.WriteLine("*/");
			}
			else
			{
				for (int i = 0; i < lines.Length; i++)
				{
					this.InnerWriter.WriteLine("// " + lines[i]);
				}
			}
		}

		/// <summary>
		/// Generates a string representation of the given value.
		/// </summary>
		/// <param name="value">The value to serialize.</param>
		public static string Serialize(JsonValue value)
		{
			return Serialize(value, JsonOptions.Default);
		}

		/// <summary>
		/// Generates a JSON string representation of the given value.
		/// </summary>
		/// <param name="value">The value to serialize.</param>
		/// <param name="options">The JsonOptions instance for serializing this object.</param>
		public static string Serialize(JsonValue value, JsonOptions options)
		{
			using (var stringWriter = new StringWriter())
			{
				var jsonWriter = new JsonWriter(stringWriter, options);

				jsonWriter.Write(value);

				return stringWriter.ToString();
			}
		}

		/// <summary>
		/// Returns a string containing the characters written to the current <see cref="JsonWriter"/> so far.
		/// </summary>
		public override string ToString()
		{
			return this.InnerWriter.ToString() ?? "";
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.InnerWriter?.Dispose();
		}
	}
}
