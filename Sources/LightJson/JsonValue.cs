using System;
using System.Diagnostics;
using System.Collections.Generic;
using LightJson.Serialization;
using System.Reflection;
using System.Collections;

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

		/// <summary>
		/// Represents a null JsonValue.
		/// </summary>
		public static readonly JsonValue Null = new JsonValue(JsonValueType.Null, default(double), null);

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
		/// Gets a value indicating whether this JsonValue is Null.
		/// </summary>
		public bool IsNull
		{
			get
			{
				return this.Type == JsonValueType.Null;
			}
		}

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

		public T? As<T>()
		{
			if (Is<T>(out T? output))
			{
				return output;
			} else
			{
				return ThrowOrNull<T>();
			}
		}

		public bool Is<T>(out T? output)
		{
			Type tType = typeof(T);

			if (IsNull)
			{
				output = default;
				return false;
			}

			if (tType == typeof(bool))
			{
				if (IsBoolean)
				{
					output = (T)(object)(this.value == 1 ? true : false);
					return true;
				}
				else
				{
					output = default;
					return false;
				}
			}
			if (tType == typeof(string))
			{
				if (IsString)
				{
					output = (T)this.reference;
					return true;
				}
				else
				{
					output = default;
					return false;
				}
			}
			else if (tType == typeof(int)
				 || tType == typeof(byte)
				 || tType == typeof(short)
				 || tType == typeof(ushort)
				 || tType == typeof(uint)
				 || tType == typeof(sbyte)
				 || tType == typeof(long)
				 || tType == typeof(ulong)
				 || tType == typeof(decimal)
				 || tType == typeof(double)
				 || tType == typeof(float))
			{
				if (IsNumber)
				{
					output = (T)(object)this.value;
					return true;
				}
				else
				{
					output = default;
					return false;
				}
			}

			foreach (var mapper in JsonOptions.Mappers)
			{
				if (mapper.CanDeserialize(this))
				{
					object result = mapper.Deserialize(this);
					output = (T)result;
					return true;
				}
			}

			output = default;
			return false;
		}

		/// <summary>
		/// Gets this value as a Boolean type.
		/// </summary>
		public bool AsBoolean
		{
			get
			{
				switch (this.Type)
				{
					case JsonValueType.Boolean:
						return (this.value == 1);

					// this shoulnd't implicit check if value is defined in order to return an
					// boolean value. instead, it should just check if the value is an boolean,
					// in order to return an boolean.

					//case JsonValueType.Number:
					//    return (this.value != 0);

					//case JsonValueType.String:
					//    return ((string)this.reference != "");

					//case JsonValueType.Object:
					//case JsonValueType.Array:
					//    return true;

					default:
						return ThrowOrNull<bool>();
				}
			}
		}

		/// <summary>
		/// Gets this value as an Integer type.
		/// </summary>
		public int AsInteger
		{
			get
			{
				var value = this.AsNumber;

				if (value >= int.MaxValue)
				{
					throw new OverflowException("The value in the JSON content is too big or too small for an Int32.");
				}
				if (value <= int.MinValue)
				{
					throw new OverflowException("The value in the JSON content is too big or too small for an Int32.");
				}

				return (int)value;
			}
		}

		/// <summary>
		/// Gets this value as a Number type.
		/// </summary>
		public double AsNumber
		{
			get
			{
				switch (this.Type)
				{
					case JsonValueType.Boolean:
						return (this.value == 1)
							? 1
							: 0;

					case JsonValueType.Number:
						return this.value;

					case JsonValueType.String:
						double number;
						if (double.TryParse((string)this.reference, out number))
						{
							return number;
						}
						else
						{
							goto default;
						}

					default:
						return ThrowOrNull<double>();
				}
			}
		}

		/// <summary>
		/// Gets this value as a String type.
		/// </summary>
		public string? AsString
		{
			get
			{
				switch (this.Type)
				{
					case JsonValueType.Boolean:
						return (this.value == 1)
							? "true"
							: "false";

					case JsonValueType.Number:
						return this.value.ToString();

					case JsonValueType.String:
						return (string)this.reference;

					case JsonValueType.Null:
						return null;

					default:
						return ThrowOrNull<string>();
				}
			}
		}

		/// <summary>
		/// Gets this value as an JsonObject.
		/// </summary>
		public JsonObject? AsJsonObject
		{
			get
			{
				return (this.IsJsonObject)
					? (JsonObject)this.reference
					: ThrowOrNull<JsonObject>();
			}
		}

		/// <summary>
		/// Gets this value as an JsonArray.
		/// </summary>
		public JsonArray? AsJsonArray
		{
			get
			{
				return (this.IsJsonArray)
					? (JsonArray)this.reference
					: ThrowOrNull<JsonArray>();
			}
		}

		/// <summary>
		/// Gets this (inner) value as a System.object.
		/// </summary>
		public object? AsObject
		{
			get
			{
				switch (this.Type)
				{
					case JsonValueType.Boolean:
					case JsonValueType.Number:
						return this.value;

					case JsonValueType.String:
					case JsonValueType.Object:
					case JsonValueType.Array:
						return this.reference;

					default:
						return null;
				}
			}
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
					throw new InvalidOperationException("This value does not represent a JsonObject.");
				}
			}
			set
			{
				if (this.IsJsonObject)
				{
					((JsonObject)this.reference)[key] = value;
				}
				else
				{
					throw new InvalidOperationException("This value does not represent a JsonObject.");
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
					throw new InvalidOperationException("This value does not represent a JsonArray.");
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
					throw new InvalidOperationException("This value does not represent a JsonArray.");
				}
			}
		}

		private T? ThrowOrNull<T>()
		{
			return JsonOptions.ThrowOnInvalidCast ?
				throw new InvalidCastException($"Cannot cast an value of type {Type} into {typeof(T).Name}.")
				: default;
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
			else if (value is string)
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
				if (mapper.CanSerialize(value))
				{
					var result = mapper.Serialize(value);
					valueType = result.Type;
					return result;
				}
			}

			if (value.GetType().IsArray)
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
				PropertyInfo[] valueProperties = value.GetType()
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
		public JsonValue(object value)
		{
			JsonValue _value = DetermineSingle(value, 0, out JsonValueType valueType);

			this.type = valueType;
			this.reference = _value.reference;
			this.value = _value.value;
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Boolean value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(bool? value)
		{
			if (value.HasValue)
			{
				this.reference = null!;

				this.type = JsonValueType.Boolean;

				this.value = value.Value ? 1 : 0;
			}
			else
			{
				this = JsonValue.Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Number value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(double? value)
		{
			if (value.HasValue)
			{
				this.reference = null!;

				this.type = JsonValueType.Number;

				this.value = value.Value;
			}
			else
			{
				this = JsonValue.Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a String value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(string value)
		{
			if (value is not null)
			{
				this.value = default(double);

				this.type = JsonValueType.String;

				this.reference = value;
			}
			else
			{
				this = JsonValue.Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a JsonObject.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonObject value)
		{
			if (value is not null)
			{
				this.value = default(double);

				this.type = JsonValueType.Object;

				this.reference = value;
			}
			else
			{
				this = JsonValue.Null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the JsonValue struct, representing a Array reference value.
		/// </summary>
		/// <param name="value">The value to be wrapped.</param>
		public JsonValue(JsonArray value)
		{
			if (value is not null)
			{
				this.value = default(double);

				this.type = JsonValueType.Array;

				this.reference = value;
			}
			else
			{
				this = JsonValue.Null;
			}
		}

		/// <summary>
		/// Converts the given nullable boolean into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(bool? value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given nullable double into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(double? value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given string into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(string value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given JsonObject into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(JsonObject value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given JsonArray into a JsonValue.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		public static implicit operator JsonValue(JsonArray value)
		{
			return new JsonValue(value);
		}

		/// <summary>
		/// Converts the given JsonValue into an Int.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator int(JsonValue jsonValue)
		{
			if (jsonValue.IsInteger)
			{
				return jsonValue.AsInteger;
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to Int32.");
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a nullable Int.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		/// <exception cref="System.InvalidCastException">
		/// Throws System.InvalidCastException when the inner value type of the
		/// JsonValue is not the desired type of the conversion.
		/// </exception>
		public static implicit operator int?(JsonValue jsonValue)
		{
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (int)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a Bool.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator bool(JsonValue jsonValue)
		{
			if (jsonValue.IsBoolean)
			{
				return (jsonValue.value == 1);
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to Boolean.");
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a nullable Bool.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		/// <exception cref="System.InvalidCastException">
		/// Throws System.InvalidCastException when the inner value type of the
		/// JsonValue is not the desired type of the conversion.
		/// </exception>
		public static implicit operator bool?(JsonValue jsonValue)
		{
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (bool)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a Double.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator double(JsonValue jsonValue)
		{
			if (jsonValue.IsNumber)
			{
				return jsonValue.value;
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to Double.");
				}
				else
				{
					return Double.NaN;
				}
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a nullable Double.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		/// <exception cref="System.InvalidCastException">
		/// Throws System.InvalidCastException when the inner value type of the
		/// JsonValue is not the desired type of the conversion.
		/// </exception>
		public static implicit operator double?(JsonValue jsonValue)
		{
			if (jsonValue.IsNull)
			{
				return null;
			}
			else
			{
				return (double)jsonValue;
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a String.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator string?(JsonValue jsonValue)
		{
			if (jsonValue.IsString || jsonValue.IsNull)
			{
				return jsonValue.AsString;
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to String.");
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a JsonObject.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator JsonObject?(JsonValue jsonValue)
		{
			if (jsonValue.IsJsonObject || jsonValue.IsNull)
			{
				return jsonValue.reference as JsonObject;
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to JsonObject.");
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Converts the given JsonValue into a JsonArray.
		/// </summary>
		/// <param name="jsonValue">The JsonValue to be converted.</param>
		public static implicit operator JsonArray?(JsonValue jsonValue)
		{
			if (jsonValue.IsJsonArray || jsonValue.IsNull)
			{
				return jsonValue.reference as JsonArray;
			}
			else
			{
				if (JsonOptions.ThrowOnInvalidCast)
				{
					throw new InvalidCastException($"Cannot cast value of type {jsonValue.Type} to JsonArray.");
				}
				else
				{
					return null;
				}
			}
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