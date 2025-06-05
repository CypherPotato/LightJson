using LightJson.Converters;
using LightJson.Serialization;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

#nullable enable

namespace LightJson
{
	/// <summary>
	/// A wrapper object that contains a valid JSON value.
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}", Type = "JsonValue({Type})")]
	[DebuggerTypeProxy(typeof(JsonValueDebugView))]
	[JsonConverter(typeof(JsonInternalConverter))]
	public struct JsonValue : IEquatable<JsonValue>, IImplicitJsonValue
	{
		private readonly JsonValueType type = JsonValueType.Undefined;
		private readonly object reference = null!;
		private readonly double value;
		internal JsonOptions options;
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
			get => this.path;
		}

		/// <summary>
		/// Gets the type of this JsonValue.
		/// </summary>
		public JsonValueType Type
		{
			get
			{
				return this.type;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is either <see cref="JsonValueType.Null"/> or <see cref="JsonValueType.Undefined"/>.
		/// </summary>
		public bool IsNull
		{
			get
			{
				return this.Type is JsonValueType.Null or JsonValueType.Undefined;
			}
		}

		/// <summary>
		/// Gets an boolean indicating whether this <see cref="JsonValue"/> is defined or not. Does not match if this value
		/// is <see cref="JsonValueType.Null"/>.
		/// </summary>
		public bool IsDefined => this.Type != JsonValueType.Undefined;

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a Boolean.
		/// </summary>
		public bool IsBoolean
		{
			get
			{
				return this.Type == JsonValueType.Boolean;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is an integer or an long value.
		/// </summary>
		public bool IsInteger
		{
			get
			{
				if (!this.IsNumber)
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
				return this.Type == JsonValueType.Number;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a String.
		/// </summary>
		public bool IsString
		{
			get
			{
				return this.Type == JsonValueType.String;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonObject.
		/// </summary>
		public bool IsJsonObject
		{
			get
			{
				return this.Type == JsonValueType.Object;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this JsonValue is a JsonArray.
		/// </summary>
		public bool IsJsonArray
		{
			get
			{
				return this.Type == JsonValueType.Array;
			}
		}

		/// <summary>
		/// Indicates that this JSON value can be null or undefined and returns a nullable value for this value.
		/// </summary>
		/// <returns></returns>
		public JsonValue? MaybeNull()
		{
			if (this.IsNull)
			{
				return null;
			}

			return this;
		}

		/// <summary>
		/// Gets this value as an defined <see cref="JsonConverter"/>.
		/// </summary>
		/// <param name="type">The defined converted type.</param>
		/// <param name="enableConverters">Optional. Defines whether to use or not defined converters.</param>
		public object Get([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, bool enableConverters = true)
		{
			if (this.IsNull)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>))
				{
					return null!;
				}
				else
				{
					return this.ThrowInvalidCast(type);
				}
			}
			return JsonDeserializer.Deserialize(this, type, 0, enableConverters, this.options);
		}

		/// <summary>
		/// Tries to get the value as the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the value to.</typeparam>
		/// <returns>The value converted to the specified type, or <see langword="default"/> if the conversion fails.</returns>
		public T? TryGet<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
		{
			try
			{
				return (T?)this.TryGet(typeof(T));
			}
			catch
			{
				return default;
			}
		}

		/// <summary>
		/// Tries to get the value as the specified type.
		/// </summary>
		/// <param name="type">The type to convert the value to.</param>
		/// <param name="enableConverters">Optional. Defines whether to use or not defined converters.</param>
		/// <returns>The value converted to the specified type, or <see langword="default"/> if the conversion fails.</returns>
		public object? TryGet([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, bool enableConverters = true)
		{
			try
			{
				return this.MaybeNull()?.Get(type, enableConverters);
			}
			catch
			{
				return default;
			}
		}

		/// <summary>
		/// Gets this value as an defined <see cref="JsonConverter"/>.
		/// </summary>
		/// <typeparam name="T">The defined mapping type.</typeparam>
		public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
		{
			var tType = typeof(T);
			return (T)this.Get(tType);
		}

		/// <summary>
		/// Gets this value as a Boolean type.
		/// </summary>
		public bool GetBoolean()
		{
			if (this.type == JsonValueType.Boolean)
				return this.value == 1;
			return this.ThrowInvalidCast<bool>(JsonValueType.Boolean);
		}

		/// <summary>
		/// Gets this value as an Long type.
		/// </summary>
		public long GetLong()
		{
			return (long)this.GetNumber();
		}

		/// <summary>
		/// Gets this value as an Integer type.
		/// </summary>
		public int GetInteger()
		{
			return (int)this.GetNumber();
		}

		/// <summary>
		/// Gets this value as a Number type.
		/// </summary>
		public double GetNumber()
		{
			if (this.type == JsonValueType.Number)
			{
				return this.value;
			}
			else if (this.type == JsonValueType.String && this.options.AllowNumbersAsStrings)
			{
				return double.Parse((string)this.reference, CultureInfo.InvariantCulture);
			}
			return this.ThrowInvalidCast<double>(JsonValueType.Number);
		}

		/// <summary>
		/// Gets this value as a String type.
		/// </summary>
		public string GetString()
		{
			if (this.type == JsonValueType.String)
				return (string)this.reference;
			return this.ThrowInvalidCast<string>(JsonValueType.String);
		}

		/// <summary>
		/// Gets this value as an JsonObject.
		/// </summary>
		public JsonObject GetJsonObject()
		{
			if (this.type == JsonValueType.Object)
				return (JsonObject)this.reference;
			return this.ThrowInvalidCast<JsonObject>(JsonValueType.Object);
		}

		/// <summary>
		/// Gets this value as an JsonArray.
		/// </summary>
		public JsonArray GetJsonArray()
		{
			if (this.type == JsonValueType.Array)
				return (JsonArray)this.reference;
			return this.ThrowInvalidCast<JsonArray>(JsonValueType.Array);
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
				if (this.IsJsonObject)
				{
					return ((JsonObject)this.reference)[key];
				}
				else
				{
					return this.ThrowInvalidCast<JsonValue>(JsonValueType.Object);
				}
			}
			set
			{
				if (this.IsJsonObject)
				{
					_ = ((JsonObject)this.reference)[key] = value;
				}
				else
				{
					_ = this.ThrowInvalidCast<JsonValue>(JsonValueType.Object);
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
				if (this.IsJsonArray)
				{
					return ((JsonArray)this.reference)[index];
				}
				else
				{
					return this.ThrowInvalidCast<JsonValue>(JsonValueType.Array);
				}
			}
			set
			{
				if (this.IsJsonArray)
				{
					((JsonArray)this.reference)[index] = value;
				}
				else
				{
					_ = this.ThrowInvalidCast<JsonValue>(JsonValueType.Array);
				}
			}
		}

		private T ThrowInvalidCast<T>(JsonValueType expectedType)
		{
			if (!this.IsDefined)
			{
				throw new InvalidCastException($"At value {this.path} it is expected to have a {expectedType}.");
			}
			throw new InvalidCastException($"Expected to read the JSON value at {this.path} as {expectedType}, but got {this.Type} instead.");
		}

		private object ThrowInvalidCast(Type T)
		{
			if (!this.IsDefined)
			{
				throw new InvalidCastException($"At value {this.path} it is expected to have a {T.Name}.");
			}
			throw new InvalidCastException($"Expected to read the JSON value at {this.path} as {T.Name}, but got {this.Type} instead.");
		}

		internal JsonValue(JsonValueType type, double value, object? reference, JsonOptions options)
		{
			this.type = type;
			this.value = value;
			this.reference = reference!;
			this.path = "$";
			this.options = options;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct from the specified object.
		/// </summary>
		/// <param name="value">The value to be converted into an <see cref="JsonValue"/>.</param>
		/// <param name="options">Optional. Determines optional JSON options to handle the serialization.</param>
		public static JsonValue Serialize(object? value, JsonOptions? options = null)
			=> (options ?? JsonOptions.Default).Serialize(value);

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Boolean value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(bool value, JsonOptions? options = null)
		{
			this.type = JsonValueType.Boolean;
			this.value = value ? 1 : 0;
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(double value, JsonOptions? options = null)
		{
			this.type = JsonValueType.Number;
			this.value = value;
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(int value, JsonOptions? options = null)
		{
			this.type = JsonValueType.Number;
			this.value = value;
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a String value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(string value, JsonOptions? options = null)
		{
			this.type = JsonValueType.String;
			this.reference = value;
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a character value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(char value, JsonOptions? options = null)
		{
			this.type = JsonValueType.String;
			this.reference = value.ToString();
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a JsonObject.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(JsonObject value, JsonOptions? options = null)
		{
			this.type = JsonValueType.Object;
			this.reference = value;
			this.path = "$";
			this.options = options ?? JsonOptions.Default;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Array reference value.
		/// </summary> 
		/// <param name="value">The value to be wrapped.</param>
		/// <param name="options">Defines the <see cref="JsonOptions"/> instance parameters.</param>
		public JsonValue(JsonArray value, JsonOptions? options = null)
		{
			this.type = JsonValueType.Array;
			this.reference = value;
			this.path = "$";
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
		public static bool TryDeserialize(string jsonText, JsonOptions? options, out JsonValue result)
			=> (options ?? JsonOptions.Default).TryDeserialize(jsonText, out result);

		/// <summary>
		/// Tries to get an <see cref="JsonValue"/> from the specified input. This method leaves the
		/// <see cref="TextReader"/> stream open.
		/// </summary>
		/// <param name="inputStream">The input stream where the JSON input is.</param>
		/// <param name="options">Optional. The JSON options to use in the deserializer.</param>
		/// <param name="result">When this method returns, returns an <see cref="JsonValue"/> with the result of the operation.</param>
		public static bool TryDeserialize(TextReader inputStream, JsonOptions? options, out JsonValue result)
			=> (options ?? JsonOptions.Default).TryDeserialize(inputStream, out result);

		/// <summary>
		/// Returns an <typeparamref name="T"/> by parsing the given string.
		/// </summary>
		/// <param name="jsonText">The JSON-formatted string to be parsed.</param>
		/// <param name="options">Optional. Sets the JsonOptions instance to deserializing the object.</param>
		public static T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string jsonText, JsonOptions? options = null) where T : notnull
			=> (options ?? JsonOptions.Default).Deserialize<T>(jsonText);

		/// <summary>
		/// Returns a <see cref="JsonValue"/> by parsing the given string.
		/// </summary>
		/// <param name="jsonText">The JSON-formatted string to be parsed.</param>
		/// <param name="options">Optional. Sets the JsonOptions instance to deserializing the object.</param>
		public static JsonValue Deserialize(string jsonText, JsonOptions? options = null)
			=> (options ?? JsonOptions.Default).Deserialize(jsonText);

		/// <summary>
		/// Returns a value indicating whether this JsonValue is equal to the given object.
		/// </summary>
		/// <param name="obj">The object to test.</param>
		public override bool Equals(object? obj)
		{
			if (obj is JsonValue jval)
			{
				return this.Equals(jval);
			}
			else if (obj is JsonObject jobj)
			{
				return this.Equals(jobj.AsJsonValue());
			}
			else if (obj is JsonArray jarr)
			{
				return this.Equals(jarr.AsJsonValue());
			}
			return false;
		}

		/// <summary>
		/// Returns a hash code for this JsonValue.
		/// </summary>
		public override int GetHashCode()
		{
			if (this.IsNull)
			{
				return this.Type.GetHashCode();
			}
			else
			{
				return HashCode.Combine(this.type, this.value, this.reference);
			}
		}

		/// <inheritdoc/>
		public bool Equals(JsonValue other)
		{
			return (this.Type == other.Type)
				&& (this.value == other.value)
				&& Equals(this.reference, other.reference);
		}

		/// <summary>
		/// Returns an string representation of the value of this <see cref="JsonValue"/>,
		/// regardless of its type.
		/// </summary>
		public readonly string? ToValueString()
		{
			if (this.reference is { } r)
			{
				return r.ToString();
			}
			else
			{
				return this.value.ToString();
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
			return this.ToString(this.options);
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

		/// <inheritdoc/>
		public JsonValue AsJsonValue()
		{
			return this;
		}

		private class JsonValueDebugView
		{
			private JsonValue jsonValue;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonObject? ObjectView
			{
				get
				{
					if (this.jsonValue.IsJsonObject)
					{
						return (JsonObject)this.jsonValue.reference;
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
					if (this.jsonValue.IsJsonArray)
					{
						return (JsonArray)this.jsonValue.reference;
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
					return this.jsonValue.Type;
				}
			}

			public object Value
			{
				get
				{
					if (this.jsonValue.IsJsonObject)
					{
						return (JsonObject)this.jsonValue.reference;
					}
					else if (this.jsonValue.IsJsonArray)
					{
						return (JsonArray)this.jsonValue.reference;
					}
					else
					{
						return this.jsonValue;
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
		public static implicit operator JsonValue(JsonObject? value) => value?.AsJsonValue() ?? JsonValue.Null;
		/// <exclude/>
		public static implicit operator JsonValue(JsonArray? value) => value?.AsJsonValue() ?? JsonValue.Null;
		/// <exclude/>
		public static implicit operator JsonValue(JsonValue[] items) => new JsonArray(JsonOptions.Default, items).AsJsonValue();
		/// <exclude/>
		public static implicit operator HttpContent(JsonValue value) => new StringContent(value.ToString(), Encoding.UTF8, "application/json");
	}
}