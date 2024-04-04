using System;
using System.Diagnostics;
using System.Collections.Generic;
using LightJson.Serialization;
using System.Reflection;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LightJson.Converters;
using System.Reflection.Metadata;
using System.Drawing;

#nullable enable

namespace LightJson
{
	/// <summary>
	/// A wrapper object that contains a valid JSON value.
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}", Type = "JsonValue({Type})")]
	[DebuggerTypeProxy(typeof(JsonValueDebugView))]
	public struct JsonValue
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
				return this.Type == JsonValueType.Null || this.Type == JsonValueType.Undefined;
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
		/// Gets a value indicating whether this JsonValue is an Integer.
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

				return (value >= Int32.MinValue) && (value <= Int32.MaxValue) && unchecked((Int32)value) == value;
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
			if (IsNull)
			{
				return null;
			}

			return this;
		}

		/// <summary>
		/// Gets this value as an defined <see cref="JsonConverter"/>.
		/// </summary>
		/// <typeparam name="T">The defined mapping type.</typeparam>
		public T Get<T>()
		{
			var tType = typeof(T);

			if (this.IsNull)
			{
				return ThrowInvalidCast<T>();
			}

			if (tType == typeof(int) ||
				tType == typeof(long) ||
				tType == typeof(double) ||
				tType == typeof(float) ||
				tType == typeof(long))
			{
				return (T)(object)GetNumber();
			}
			else if (tType == typeof(string))
			{
				return (T)(object)GetString();
			}
			else if (tType == typeof(bool))
			{
				return (T)(object)GetBoolean();
			}
			else
			{
				foreach (var mapper in options.Converters)
				{
					if (mapper.CanSerialize(tType))
					{
						try
						{
							return (T)mapper.Deserialize(this, tType);
						}
						catch (Exception ex)
						{
							throw new InvalidOperationException($"Caught exception while trying to map {path} to {typeof(T).Name}: {ex.Message}");
						}
					}
				}
			}			

			throw new InvalidOperationException($"No converter matched the object type {typeof(T).FullName}.");
		}

		/// <summary>
		/// Gets this value as a Boolean type.
		/// </summary>
		public bool GetBoolean()
		{
			if (this.type == JsonValueType.Boolean) return (this.value == 1);
			return ThrowInvalidCast<bool>(JsonValueType.Boolean);
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
			if (this.type == JsonValueType.Number) return this.value;
			return ThrowInvalidCast<double>(JsonValueType.Number);
		}

		/// <summary>
		/// Gets this value as a String type.
		/// </summary>
		public string GetString()
		{
			if (this.type == JsonValueType.String) return (string)this.reference;
			return ThrowInvalidCast<string>(JsonValueType.String);
		}

		/// <summary>
		/// Gets this value as an JsonObject.
		/// </summary>
		public JsonObject GetJsonObject()
		{
			if (this.type == JsonValueType.Object) return (JsonObject)this.reference;
			return ThrowInvalidCast<JsonObject>(JsonValueType.Object);
		}

		/// <summary>
		/// Gets this value as an JsonArray.
		/// </summary>
		public JsonArray GetJsonArray()
		{
			if (this.type == JsonValueType.Array) return (JsonArray)this.reference;
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
				if (this.IsJsonObject)
				{
					return ((JsonObject)this.reference)[key];
				}
				else
				{
					return ThrowInvalidCast<JsonValue>(JsonValueType.Object);
				}
			}
			set
			{
				if (this.IsJsonObject)
				{
					var jobj = ((JsonObject)this.reference)[key] = value;
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
				if (this.IsJsonArray)
				{
					return ((JsonArray)this.reference)[index];
				}
				else
				{
					return ThrowInvalidCast<JsonValue>(JsonValueType.Array);
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

		private JsonValue(JsonValueType type, double value, object? reference, JsonOptions options)
		{
			this.type = type;
			this.value = value;
			this.reference = reference!;
			this.path = "$";
			this.options = options;
		}

		private static JsonValue DetermineSingle(object? value, int deepness, JsonOptions options, out JsonValueType valueType)
		{
			if (deepness > 128)
			{
				throw new InvalidOperationException("The maximum JSON depth level has been reached.");
			}
			if (value is null)
			{
				valueType = JsonValueType.Null;
				return new JsonValue(valueType, 0, null, options);
			}

			var itemType = value.GetType();

			if (value is string || value is char)
			{
				valueType = JsonValueType.String;
				return new JsonValue(valueType, 0, value.ToString(), options);
			}
			else if (value is int nint)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, nint, null, options);
			}
			else if (value is byte || value is sbyte || value is uint || value is long || value is ulong)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, Convert.ToInt64(value), null, options);
			}
			else if (value is double ndbl)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, ndbl, null, options);
			}
			else if (value is float || value is decimal)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, Convert.ToDouble(value), null, options);
			}
			else if (value is bool nbool)
			{
				valueType = JsonValueType.Boolean;
				return new JsonValue(valueType, nbool ? 1 : 0, null, options);
			}

			foreach (var mapper in options.Converters)
			{
				if (mapper.CanSerialize(itemType))
				{
					var result = mapper.Serialize(value);
					valueType = result.Type;
					return result;
				}
			}

			if (itemType.IsAssignableTo(typeof(IEnumerable)))
			{
				JsonArray arr = new JsonArray(options);
				foreach (object? item in (IEnumerable)value)
				{
					if (item == null) continue;
					arr.Add(DetermineSingle(item, deepness + 1, options, out _));
				}

				valueType = JsonValueType.Array;
				return new JsonValue(valueType, 0, arr, options);
			}
			else
			{
				JsonObject newObj = new JsonObject(options);
				PropertyInfo[] valueProperties = itemType
					.GetProperties(BindingFlags.Public | BindingFlags.Instance);

				foreach (PropertyInfo property in valueProperties)
				{
					var atrJsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>();
					var atrJsonProperty = property.GetCustomAttribute<JsonPropertyNameAttribute>();

					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.Always)
						continue;

					string name = atrJsonProperty?.Name ?? property.Name;
					object? v = property.GetValue(value);

					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingNull && v is null)
						continue;
					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingDefault && v == default)
						continue;

					JsonValue jsonValue = DetermineSingle(v, deepness + 1, options, out _);
					newObj.Add(name, jsonValue);
				}

				if (options.SerializeFields)
				{
					FieldInfo[] fields = itemType
						.GetFields(BindingFlags.Public | BindingFlags.Instance);

					foreach (FieldInfo field in fields)
					{
						var atrJsonIgnore = field.GetCustomAttribute<JsonIgnoreAttribute>();
						var atrJsonProperty = field.GetCustomAttribute<JsonPropertyNameAttribute>();

						if (atrJsonIgnore?.Condition == JsonIgnoreCondition.Always)
							continue;

						string name = atrJsonProperty?.Name ?? field.Name;
						object? v = field.GetValue(value);

						if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingNull && v is null)
							continue;
						if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingDefault && v == default)
							continue;

						JsonValue jsonValue = DetermineSingle(v, deepness + 1, options, out _);
						newObj.Add(name, jsonValue);
					}
				}

				valueType = JsonValueType.Object;
				return new JsonValue(JsonValueType.Object, 0, newObj, options);
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct from the specified object.
		/// </summary>
		/// <param name="value">The value to be converted into an <see cref="JsonValue"/>.</param>
		/// <param name="options">Optional. Determines optional JSON options to handle the serialization.</param>
		public static JsonValue Serialize(object? value, JsonOptions? options = null)
		{
			JsonOptions _opt = options ?? JsonOptions.Default;
			JsonValue _value = DetermineSingle(value, 0, _opt, out JsonValueType valueType);
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
		/// Initializes a new instance of the JsonValue struct, representing a String value.
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
			return (a.Type == b.Type)
				&& (a.value == b.value)
				&& Equals(a.reference, b.reference);
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
			if (obj is null)
			{
				return this.IsNull;
			}

			var jsonValue = obj as JsonValue?;

			if (jsonValue.HasValue)
			{
				return (this == jsonValue.Value);
			}
			else
			{
				return false;
			}
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
				return this.Type.GetHashCode()
					^ this.value.GetHashCode()
					^ EqualityComparer<object>.Default.GetHashCode(this.reference);
			}
		}

		/// <summary>
		/// Returns an string representation of the value of this <see cref="JsonValue"/>,
		/// regardless of its type.
		/// </summary>
		public readonly string? ToValueString()
		{
			return reference.ToString() ?? value.ToString();
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
	}
}