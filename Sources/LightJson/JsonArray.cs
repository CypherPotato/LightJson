using LightJson.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace LightJson
{
	/// <summary>
	/// Represents an ordered collection of JsonValues.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[JsonConverter(typeof(JsonArrayInternalConverter))]
	public sealed class JsonArray : IEnumerable<JsonValue>, IList<JsonValue>, IImplicitJsonValue, IReadOnlyList<JsonValue>, IEquatable<JsonArray>, IEquatable<JsonValue>
	{
		internal string path = "";
		private readonly IList<JsonValue> items;
		private readonly JsonOptions options;

		/// <inheritdoc/>
		public int Count
		{
			get
			{
				return this.items.Count;
			}
		}

		/// <inheritdoc/>
		public bool IsReadOnly => this.items.IsReadOnly;

		/// <summary>
		/// Gets or sets the value at the given index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <remarks>
		/// The getter will return <see cref="JsonValue.Undefined"/> if the given index is out of range.
		/// </remarks>
		public JsonValue this[int index]
		{
			get
			{
				JsonValue val;
				if (index >= 0 && index < this.items.Count)
				{
					val = this.items[index];
				}
				else
				{
					val = JsonValue.Undefined;
				}
				val.path = this.path + $"[{index}]";
				return val;
			}
			set
			{
				this.items[index] = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of JsonArray.
		/// </summary>
		public JsonArray() : this(JsonOptions.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of JsonArray with the specified <see cref="JsonOptions"/>.
		/// </summary>
		public JsonArray(JsonOptions options)
		{
			this.items = [];
			this.options = options;
		}

		/// <summary>
		/// Initializes a new instance of JsonArray, adding the given values to the collection.
		/// </summary>
		/// <param name="options">Specifies the <see cref="JsonOptions"/>.</param>
		/// <param name="values">The values to be added to this collection.</param>
		public JsonArray(JsonOptions options, params JsonValue[] values) : this(options)
		{
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			foreach (var value in values)
			{
				this.items.Add(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of JsonArray with the specified <see cref="JsonOptions"/> and values.
		/// </summary>
		/// <param name="options">Specifies the <see cref="JsonOptions"/> used for this JsonArray.</param>
		/// <param name="values">The collection of <see cref="JsonValue"/> to be added to this JsonArray.</param>
		public JsonArray(JsonOptions options, IEnumerable<object?> values) : this(options)
		{
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			foreach (var value in values)
			{
				this.items.Add(options.Serialize(value));
			}
		}

		/// <summary>
		/// Initializes a new instance of JsonArray with the specified values.
		/// </summary>
		/// <param name="values">The collection of objects to be added to this JsonArray.</param>
		public JsonArray(IEnumerable<object?> values) : this(JsonOptions.Default)
		{
			foreach (var value in values)
			{
				this.items.Add(this.options.Serialize(value));
			}
		}

		/// <summary>
		/// Returns an <see cref="JsonValue"/> representating this <see cref="JsonArray"/>.
		/// </summary>
		/// <returns></returns>
		public JsonValue AsJsonValue() => new JsonValue(this, this.options);

		/// <inheritdoc/>
		public IEnumerator<JsonValue> GetEnumerator()
		{
			return this.items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.items).GetEnumerator();
		}

		/// <inheritdoc/>
		public int IndexOf(JsonValue item)
		{
			return this.items.IndexOf(item);
		}

		/// <inheritdoc/>
		public void Insert(int index, JsonValue item)
		{
			this.items.Insert(index, item);
		}

		/// <inheritdoc/>
		public void RemoveAt(int index)
		{
			this.items.RemoveAt(index);
		}

		/// <inheritdoc/>
		public void Add(JsonValue item)
		{
			this.items.Add(item);
		}

		/// <summary>
		/// Adds a value to this array.
		/// </summary>
		/// <param name="value">The value to be serialized into a JSON value.</param>
		public void Add(object? value)
		{
			this.Add(this.options.Serialize(value));
		}

		/// <summary>
		/// Adds a range of values to this array.
		/// </summary>
		/// <param name="items">The items to add.</param>
		public void AddRange(IEnumerable<object?> items)
		{
			foreach (var item in items)
			{
				this.Add(item);
			}
		}

		/// <summary>
		/// Adds a range of values to this array.
		/// </summary>
		/// <param name="items">The items to add.</param>
		public void AddRange(IEnumerable<JsonValue> items)
		{
			foreach (var item in items)
			{
				this.Add(item);
			}
		}

		/// <inheritdoc/>
		public void Clear()
		{
			this.items.Clear();
		}

		/// <inheritdoc/>
		public bool Contains(JsonValue item)
		{
			return this.items.Contains(item);
		}

		/// <inheritdoc/>
		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			this.items.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc/>
		public bool Remove(JsonValue item)
		{
			return this.items.Remove(item);
		}

		/// <summary>
		/// Casts every <see cref="JsonValue"/> in this array into an <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to cast the JsonValue into.</typeparam>
		public IEnumerable<T> EveryAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull
		{
			foreach (var jsonitem in this.items)
			{
				yield return jsonitem.Get<T>();
			}
		}

		/// <summary>
		/// Casts every <see cref="JsonValue"/> in this array into an <typeparamref name="T"/>.
		/// This method also includes null values.
		/// </summary>
		/// <typeparam name="T">The type to cast the JsonValue into.</typeparam>
		public IEnumerable<T?> EveryAsNullable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull
		{
			foreach (var jsonitem in this.items)
			{
				if (jsonitem.IsNull)
				{
					yield return default;
				}
				else
				{
					yield return jsonitem.Get<T>();
				}
			}
		}

		/// <summary>
		/// Converts the current object to an array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the resulting array. Must be a non-nullable type.</typeparam>
		/// <returns>An array of type <typeparamref name="T"/> containing the elements.</returns>
		public T[] ToArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull
			=> this.EveryAs<T>().ToArray();

		/// <summary>
		/// Converts the current <see cref="JsonArray"/> to an array of type <typeparamref name="T"/> using the provided conversion function.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the resulting array.</typeparam>
		/// <param name="func">A function that converts a <see cref="JsonValue"/> to an object of type <typeparamref name="T"/>.</param>
		/// <returns>An array of type <typeparamref name="T"/> containing the converted elements.</returns>
		public T[] ToArray<T>(Func<JsonValue, T> func)
		{
			var result = new T[this.Count];
			for (int i = 0; i < this.Count; i++)
			{
				result[i] = func(this[i]);
			}
			return result;
		}

		/// <summary>
		/// Returns a JSON string representing the state of this value.
		/// </summary>
		public override string ToString()
		{
			return this.ToString(JsonOptions.Default);
		}

		/// <inheritdoc />
		public string ToString(JsonOptions options)
		{
			return JsonWriter.Serialize(this.AsJsonValue(), options);
		}

		/// <inheritdoc />
		public bool Equals(JsonArray? other)
		{
			if (other is null)
				return false;
			return other.items.SequenceEqual(this.items);
		}

		/// <inheritdoc />
		public bool Equals(JsonValue other)
		{
			return other.Type == JsonValueType.Array && this.Equals(other.GetJsonArray());
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			if (obj is JsonArray jarr)
			{
				return this.Equals(jarr);
			}
			else if (obj is JsonValue jval)
			{
				return this.Equals(jval);
			}
			return false;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			int carry = UInt16.MaxValue;
			foreach (var item in this.items)
			{
				carry ^= HashCode.Combine(item);
			}
			return carry;
		}

		/// <exclude/>
		public static implicit operator HttpContent(JsonArray value) => new StringContent(value.ToString(), Encoding.UTF8, "application/json");
	}
}
