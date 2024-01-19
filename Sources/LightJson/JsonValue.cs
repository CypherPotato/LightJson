using System;
using System.Diagnostics;
using System.Collections.Generic;
using LightJson.Serialization;
using System.Reflection;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using LightJson.Converters;
using System.Reflection.Metadata;


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
		private readonly JsonValueType type;
		private readonly object reference = null!;
		private readonly double value;
		internal string path;

		/// <summary>
		/// Represents a null <see cref="JsonValue"/>.
		/// </summary>
		public static readonly JsonValue Null = new JsonValue(JsonValueType.Null, default(double), null);

		/// <summary>
		/// Represents an <see cref="JsonValue"/> that wasn't defined in any JSON document.
		/// </summary>
		public static readonly JsonValue Undefined = new JsonValue(JsonValueType.Undefined, default(double), null);

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
			if (this.IsNull)
			{
				return ThrowInvalidCast<T>();
			}

			Type tType = typeof(T);

			foreach (var mapper in JsonOptions.Mappers)
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

		/// <summary>
		/// Initializes a new instance of the JsonValue struct.
		/// </summary>
		/// <param name="type">The Json type of the JsonValue.</param>
		/// <param name="value">
		/// The internal value of the JsonValue.
		/// This is used when the Json type is Number or Boolean.
		/// </param>
		/// <param name="reference">
		/// The internal value reference of the JsonValue.
		/// This value is used when the Json type is String, JsonObject, or JsonArray.
		/// </param>
		private JsonValue(JsonValueType type, double value, object? reference)
		{
			this.type = type;
			this.value = value;
			this.reference = reference!;
			this.path = "";
		}

		private static JsonValue DetermineSingle(object? value, int deepness, out JsonValueType valueType)
		{
			if (deepness > 128)
			{
				throw new InvalidOperationException("The maximum JSON depth level has been reached.");
			}
			if (value is null)
			{
				valueType = JsonValueType.Null;
				return new JsonValue(valueType, 0, null);
			}

			var itemType = value.GetType();

			if (value is string)
			{
				valueType = JsonValueType.String;
				return new JsonValue(valueType, 0, value);
			}
			else if (value is int nint)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, nint, null);
			}
			else if (value is double ndbl)
			{
				valueType = JsonValueType.Number;
				return new JsonValue(valueType, ndbl, null);
			}
			else if (value is bool nbool)
			{
				valueType = JsonValueType.Boolean;
				return new JsonValue(valueType, nbool ? 1 : 0, null);
			}

			foreach (var mapper in JsonOptions.Mappers)
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
				JsonArray arr = new JsonArray();
				foreach (object? item in (IEnumerable)value)
				{
					if (item == null) continue;
					arr.Add(DetermineSingle(item, deepness + 1, out _));
				}

				valueType = JsonValueType.Array;
				return new JsonValue(valueType, 0, arr);
			}
			else
			{
				JsonObject newObj = new JsonObject();
				PropertyInfo[] valueProperties = itemType
					.GetProperties(BindingFlags.Public | BindingFlags.Instance);

				foreach (PropertyInfo property in valueProperties)
				{
					string name = property.Name;
					object? v = property.GetValue(value);
					JsonValue jsonValue = DetermineSingle(v, deepness + 1, out _);
					newObj.Add(name, jsonValue);
				}

				valueType = JsonValueType.Object;
				return new JsonValue(JsonValueType.Object, 0, newObj);
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a dynamic value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public static JsonValue FromObject(object? value)
		{
			JsonValue _value = DetermineSingle(value, 0, out JsonValueType valueType);
			return _value;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Boolean value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(bool value)
		{
			this.type = JsonValueType.Boolean;
			this.value = value ? 1 : 0;
			this.path = "";
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(double value)
		{
			this.type = JsonValueType.Number;
			this.value = value;
			this.path = "";
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(int value)
		{
			this.type = JsonValueType.Number;
			this.value = value;
			this.path = "";
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a String value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(string value)
		{
			this.type = JsonValueType.String;
			this.reference = value;
			this.path = "";
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a JsonObject.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonObject value)
		{
			this.type = JsonValueType.Object;
			this.reference = value;
			this.path = "";
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Array reference value.
		/// </summary> 
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonArray value)
		{
			this.type = JsonValueType.Array;
			this.reference = value;
			this.path = "";
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
		/// Returns a JsonValue by parsing the given string.
		/// </summary>
		/// <param name="text">The JSON-formatted string to be parsed.</param>
		public static JsonValue Parse(string text)
		{
			return JsonReader.Parse(text);
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
		/// Returns a JSON string representing the state of the object.
		/// </summary>
		/// <remarks>
		/// The resulting string is safe to be inserted as is into dynamically
		/// generated JavaScript or JSON code.
		/// </remarks>
		public override string ToString()
		{
			return ToString(false);
		}

		/// <summary>
		/// Returns a JSON string representing the state of the object.
		/// </summary>
		/// <remarks>
		/// The resulting string is safe to be inserted as is into dynamically
		/// generated JavaScript or JSON code.
		/// </remarks>
		/// <param name="pretty">
		/// Indicates whether the resulting string should be formatted for human-readability.
		/// </param>
		public string ToString(bool pretty)
		{
			return JsonWriter.Serialize(this, pretty);
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