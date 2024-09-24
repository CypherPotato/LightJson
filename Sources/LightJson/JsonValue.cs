using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using LightJson.Converters;
using LightJson.Serialization;

#nullable enable

namespace LightJson
{
	/// <summary>
	/// A wrapper object that contains a valid JSON value.
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}", Type = "JsonValue({Type})")]
	[DebuggerTypeProxy(typeof(JsonValueDebugView))]
	public struct JsonValue : IEquatable<JsonValue>
	{
		private readonly JsonValueType type = JsonValueType.Undefined;
		private readonly object reference = null!;
		private readonly double value;
		private JsonOptions options;
		internal string path;

		/// <summary>
		/// Represents a null <see cref="JsonValue"/>.
		/// </summary>
		public static readonly JsonValue Null = new JsonValue(JsonValueType.Null, default(double), null, JsonOptions.Default);

		/// <summary>
		/// Represents an <see cref="JsonValue"/> that wasn't defined in any JSON document.
		/// </summary>
		public static readonly JsonValue Undefined = new JsonValue(JsonValueType.Undefined, default(double), null, JsonOptions.Default);

		/// <summary>
		/// Gets the JSON path of this JsonValue.
		/// </summary>
		public string Path
		{
			get => path;
		}

		/// <summary>
		/// Gets the type of this JsonValue.
		/// </summary>
		public JsonValueType Type
		{
			get
			{
				return type;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is either <see cref="JsonValueType.Null"/> or <see cref="JsonValueType.Undefined"/>.
		/// </summary>
		public bool IsNull
		{
			get
			{
				return Type is JsonValueType.Null or JsonValueType.Undefined;
			}
		}

		/// <summary>
		/// Gets an boolean indicating whether this <see cref="JsonValue"/> is defined or not. Does not match if this value
		/// is <see cref="JsonValueType.Null"/>.
		/// </summary>
		public bool IsDefined => Type != JsonValueType.Undefined;

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a Boolean.
		/// </summary>
		public bool IsBoolean
		{
			get
			{
				return Type == JsonValueType.Boolean;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is an integer or an long value.
		/// </summary>
		public bool IsInteger
		{
			get
			{
				if (!IsNumber)
				{
					return false;
				}

				var value = this.value;

				return (value >= Int64.MinValue) && (value <= Int64.MaxValue) && unchecked((Int64)value) == value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a Number.
		/// </summary>
		public bool IsNumber
		{
			get
			{
				return Type == JsonValueType.Number;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a String.
		/// </summary>
		public bool IsString
		{
			get
			{
				return Type == JsonValueType.String;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonObject.
		/// </summary>
		public bool IsJsonObject
		{
			get
			{
				return Type == JsonValueType.Object;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonArray.
		/// </summary>
		public bool IsJsonArray
		{
			get
			{
				return Type == JsonValueType.Array;
			}
		}

		/// <summary>
		/// Indicates that this JSON value can be null or undefined and returns a nullable value for this value.
		/// </summary>
		/// <returns></returns>
		public JsonValue? MaybeNull()
		{
			if (IsNull)
			{
				return null;
			}

			return this;
		}

		/// <summary>
		/// Gets this value as an defined <see cref="JsonConverter"/>.
		/// </summary>
		/// <param name="type">The defined converted type.</param>
		public object Get(Type type)
		{
			if (IsNull)
			{
				return ThrowInvalidCast(type);
			}
			return Dynamic.DeserializeObject(this, type, options);
		}

		/// <summary>
		/// Gets this value as an defined <see cref="JsonConverter"/>.
		/// </summary>
		/// <typeparam name="T">The defined mapping type.</typeparam>
		public T Get<T>() where T : notnull
		{
			var tType = typeof(T);
			return (T)Get(tType);
		}

		/// <summary>
		/// Gets this value as a Boolean type.
		/// </summary>
		public bool GetBoolean()
		{
			if (type == JsonValueType.Boolean) return (value == 1);
			return ThrowInvalidCast<bool>(JsonValueType.Boolean);
		}

		/// <summary>
		/// Gets this value as an Long type.
		/// </summary>
		public long GetLong()
		{
			return (long)GetNumber();
		}

		/// <summary>
		/// Gets this value as an Integer type.
		/// </summary>
		public int GetInteger()
		{
			return (int)GetNumber();
		}

		/// <summary>
		/// Gets this value as a Number type.
		/// </summary>
		public double GetNumber()
		{
			if (type == JsonValueType.Number) return value;
			return ThrowInvalidCast<double>(JsonValueType.Number);
		}

		/// <summary>
		/// Gets this value as a String type.
		/// </summary>
		public string GetString()
		{
			if (type == JsonValueType.String) return (string)reference;
			return ThrowInvalidCast<string>(JsonValueType.String);
		}

		/// <summary>
		/// Gets this value as an JsonObject.
		/// </summary>
		public JsonObject GetJsonObject()
		{
			if (type == JsonValueType.Object) return (JsonObject)reference;
			return ThrowInvalidCast<JsonObject>(JsonValueType.Object);
		}

		/// <summary>
		/// Gets this value as an JsonArray.
		/// </summary>
		public JsonArray GetJsonArray()
		{
			if (type == JsonValueType.Array) return (JsonArray)reference;
			return ThrowInvalidCast<JsonArray>(JsonValueType.Array);
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown when this JsonValue is not a JsonObject.
		/// </exception> 
		public JsonValue this[string key]
		{
			get
			{
				if (IsJsonObject)
				{
					return ((JsonObject)reference)[key];
				}
				else
				{
					return ThrowInvalidCast<JsonValue>(JsonValueType.Object);
				}
			}
			set
			{
				if (IsJsonObject)
				{
					var jobj = ((JsonObject)reference)[key] = value;
				}
				else
				{
					ThrowInvalidCast<JsonValue>(JsonValueType.Object);
				}
			}
		}

		/// <summary>
		/// Gets or sets the value at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown when this JsonValue is not a JsonArray
		/// </exception>
		public JsonValue this[int index]
		{
			get
			{
				if (IsJsonArray)
				{
					return ((JsonArray)reference)[index];
				}
				else
				{
					return ThrowInvalidCast<JsonValue>(JsonValueType.Array);
				}
			}
			set
			{
				if (IsJsonArray)
				{
					((JsonArray)reference)[index] = value;
				}
				else
				{
					ThrowInvalidCast<JsonValue>(JsonValueType.Array);
				}
			}
		}

		private T ThrowInvalidCast<T>(JsonValueType expectedType)
		{
			if (!IsDefined)
			{
				throw new InvalidCastException($"At value {path} it is expected to have a {expectedType}.");
			}
			throw new InvalidCastException($"Expected to read the JSON value at {path} as {expectedType}, but got {Type} instead.");
		}

		private T ThrowInvalidCast<T>()
		{
			if (!IsDefined)
			{
				throw new InvalidCastException($"At value {path} it is expected to have a {typeof(T).Name}.");
			}
			throw new InvalidCastException($"Expected to read the JSON value at {path} as {typeof(T).Name}, but got {Type} instead.");
		}

		private object ThrowInvalidCast(Type T)
		{
			if (!IsDefined)
			{
				throw new InvalidCastException($"At value {path} it is expected to have a {T.Name}.");
			}
			throw new InvalidCastException($"Expected to read the JSON value at {path} as {T.Name}, but got {Type} instead.");
		}

		internal JsonValue(JsonValueType type, double value, object? reference, JsonOptions options)
		{
			this.type = type;
			this.value = value;
			this.reference = reference!;
			path = "$";
			this.options = options;
		}

		/// <summary>
		/// Serializes an dynamic object without using any defined <see cref="Converters.JsonConverter"/>.
		/// </summary>
		/// <param name="value">The complex object to serialize.</param>
		/// <param name="options">Optional. Determines optional JSON options to handle the serialization.</param>
		public static JsonValue SerializeDynamicObject(object value, JsonOptions? options = null)
		{
			JsonOptions _opt = options ?? JsonOptions.Default;
			return Dynamic.SerializeObject(value, 0, true, _opt, out JsonValueType valueType);
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct from the specified object.
		/// </summary>
		/// <param name="value">The value to be converted into an <see cref="JsonValue"/>.</param>
		/// <param name="options">Optional. Determines optional JSON options to handle the serialization.</param>
		public static JsonValue Serialize(object? value, JsonOptions? options = null)
		{
			JsonOptions _opt = options ?? JsonOptions.Default;
			JsonValue _value = Dynamic.SerializeObject(value, 0, true, _opt, out JsonValueType valueType);
			_value.options = _opt;
			return _value;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Boolean value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(bool value, JsonOptions? options = null)
		{
			type = JsonValueType.Boolean;
			this.value = value ? 1 : 0;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(double value, JsonOptions? options = null)
		{
			type = JsonValueType.Number;
			this.value = value;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(int value, JsonOptions? options = null)
		{
			type = JsonValueType.Number;
			this.value = value;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a String value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(string value, JsonOptions? options = null)
		{
			type = JsonValueType.String;
			reference = value;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a character value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(char value, JsonOptions? options = null)
		{
			type = JsonValueType.String;
			reference = value.ToString();
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a JsonObject.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(JsonObject value, JsonOptions? options = null)
		{
			type = JsonValueType.Object;
			reference = value;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Array reference value.
		/// </summary> 
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(JsonArray value, JsonOptions? options = null)
		{
			type = JsonValueType.Array;
			reference = value;
			path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Returns a value indicating whether the two given JsonValues are equal.
		/// </summary>
		/// <param name="a">A JsonValue to compare.</param>
		/// <param name="b">A JsonValue to compare.</param>
		public static bool operator ==(JsonValue a, JsonValue b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Returns a value indicating whether the two given JsonValues are unequal.
		/// </summary>
		/// <param name="a">A JsonValue to compare.</param>
		/// <param name="b">A JsonValue to compare.</param>
		public static bool operator !=(JsonValue a, JsonValue b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Tries to get an <see cref="JsonValue"/> from the specified input.
		/// </summary>
		/// <param name="jsonText">The JSON-formatted string.</param>
		/// <param name="options">Optional. The JSON options to use in the deserializer.</param>
		/// <param name="result">When this method returns, returns an <see cref="JsonValue"/> with the result of the operation.</param>
		public static bool TryDeserialize(ReadOnlySpan<char> jsonText, JsonOptions? options, out JsonValue result)
		{
			try
			{
				result = JsonReader.Parse(jsonText, options);
				return true;
			}
			catch
			{
				result = JsonValue.Undefined;
				return false;
			}
		}

		/// <summary>
		/// Tries to get an <see cref="JsonValue"/> from the specified input. This method leaves the
		/// <see cref="TextReader"/> stream open.
		/// </summary>
		/// <param name="inputStream">The input stream where the JSON input is.</param>
		/// <param name="options">Optional. The JSON options to use in the deserializer.</param>
		/// <param name="result">When this method returns, returns an <see cref="JsonValue"/> with the result of the operation.</param>
		public static bool TryDeserialize(TextReader inputStream, JsonOptions? options, out JsonValue result)
		{
			try
			{
				result = JsonReader.Parse(inputStream, options);
				return true;
			}
			catch
			{
				result = JsonValue.Undefined;
				return false;
			}
		}

		/// <summary>
		/// Returns an <typeparamref name="T"/> by parsing the given string.
		/// </summary>
		/// <param name="jsonText">The JSON-formatted string to be parsed.</param>
		/// <param name="options">Optional. Sets the JsonOptions instance to deserializing the object.</param>
		public static T Deserialize<T>(ReadOnlySpan<char> jsonText, JsonOptions? options = null) where T : notnull
		{
			return JsonReader.Parse(jsonText, options).Get<T>();
		}

		/// <summary>
		/// Returns a <see cref="JsonValue"/> by parsing the given string.
		/// </summary>
		/// <param name="jsonText">The JSON-formatted string to be parsed.</param>
		/// <param name="options">Optional. Sets the JsonOptions instance to deserializing the object.</param>
		public static JsonValue Deserialize(string jsonText, JsonOptions? options = null)
		{
			return JsonReader.Parse(jsonText, options);
		}

		/// <summary>
		/// Returns a value indicating whether this JsonValue is equal to the given object.
		/// </summary>
		/// <param name="obj">The object to test.</param>
		public override bool Equals(object? obj)
		{
			if (obj is JsonValue jval)
			{
				return Equals(jval);
			}
			else if (obj is JsonObject jobj)
			{
				return Equals(jobj.AsJsonValue());
			}
			else if (obj is JsonArray jarr)
			{
				return Equals(jarr.AsJsonValue());
			}
			return false;
		}

		/// <summary>
		/// Returns a hash code for this JsonValue.
		/// </summary>
		public override int GetHashCode()
		{
			if (IsNull)
			{
				return Type.GetHashCode();
			}
			else
			{
				return HashCode.Combine(type, value, reference);
			}
		}

		/// <inheritdoc/>
		public bool Equals(JsonValue other)
		{
			return (Type == other.Type)
				&& (value == other.value)
				&& Equals(reference, other.reference);
		}

		/// <summary>
		/// Returns an string representation of the value of this <see cref="JsonValue"/>,
		/// regardless of its type.
		/// </summary>
		public readonly string? ToValueString()
		{
			if (reference is { } r)
			{
				return r.ToString();
			}
			else
			{
				return value.ToString();
			}
		}

		/// <summary>
		/// Returns a JSON string representing the state of the object.
		/// </summary>
		/// <remarks>
		/// The resulting string is safe to be inserted as is into dynamically
		/// generated JavaScript or JSON code.
		/// </remarks>
		public override string ToString()
		{
			return ToString(options);
		}

		/// <summary>
		/// Returns a JSON string representing the state of the object.
		/// </summary>
		/// <remarks>
		/// The resulting string is safe to be inserted as is into dynamically
		/// generated JavaScript or JSON code.
		/// </remarks>
		/// <param name="options">Specifies the JsonOptions used to render this Json value.</param>
		public string ToString(JsonOptions options)
		{
			return JsonWriter.Serialize(this, options);
		}

		private class JsonValueDebugView
		{
			private JsonValue jsonValue;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonObject? ObjectView
			{
				get
				{
					if (jsonValue.IsJsonObject)
					{
						return (JsonObject)jsonValue.reference;
					}
					else
					{
						return null;
					}
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonArray? ArrayView
			{
				get
				{
					if (jsonValue.IsJsonArray)
					{
						return (JsonArray)jsonValue.reference;
					}
					else
					{
						return null;
					}
				}
			}

			public JsonValueType Type
			{
				get
				{
					return jsonValue.Type;
				}
			}

			public object Value
			{
				get
				{
					if (jsonValue.IsJsonObject)
					{
						return (JsonObject)jsonValue.reference;
					}
					else if (jsonValue.IsJsonArray)
					{
						return (JsonArray)jsonValue.reference;
					}
					else
					{
						return jsonValue;
					}
				}
			}

			public JsonValueDebugView(JsonValue jsonValue)
			{
				this.jsonValue = jsonValue;
			}
		}

		/// <exclude/>
		public static implicit operator JsonValue(string? value) => value is null ? JsonValue.Null : new JsonValue(value);
		/// <exclude/>
		public static implicit operator JsonValue(int value) => new JsonValue(value);
		/// <exclude/>
		public static implicit operator JsonValue(bool value) => new JsonValue(value);
		/// <exclude/>
		public static implicit operator JsonValue(double value) => new JsonValue(value);
		/// <exclude/>
		public static implicit operator JsonValue(short value) => Serialize(value);
		/// <exclude/>
		public static implicit operator JsonValue(decimal value) => Serialize(value);
		/// <exclude/>
		public static implicit operator JsonValue(long value) => Serialize(value);
		/// <exclude/>
		public static implicit operator JsonValue(char value) => new JsonValue(value);
		/// <exclude/>
		public static implicit operator JsonValue(JsonObject value) => value.AsJsonValue();
		/// <exclude/>
		public static implicit operator JsonValue(JsonArray value) => value.AsJsonValue();
		/// <exclude/>
		public static implicit operator JsonValue(JsonValue[] items) => new JsonArray(JsonOptions.Default, items).AsJsonValue();
	}
}