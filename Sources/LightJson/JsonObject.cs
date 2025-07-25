using LightJson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace LightJson
{
	/// <summary>
	/// Represents a key-value pair collection of <see cref="JsonValue"/> objects.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[JsonConverter(typeof(JsonObjectInternalConverter))]
	public sealed class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>, IDictionary<string, JsonValue>, IImplicitJsonValue
	{
		internal string path;
		internal readonly IDictionary<string, JsonValue> properties;
		private readonly JsonOptions options;

		/// <summary> 
		/// Gets all defined properties in this <see cref="JsonObject"/>.
		/// </summary>
		public IDictionary<string, JsonValue> Properties { get => this.properties; }

		/// <summary>
		/// Gets the number of properties in this JsonObject.
		/// </summary>
		public int Count
		{
			get
			{
				return this.properties.Count;
			}
		}

		/// <inheritdoc/>
		public ICollection<string> Keys => this.properties.Keys;

		/// <inheritdoc/>
		public ICollection<JsonValue> Values => this.properties.Values;

		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets or sets the property with the given key.
		/// </summary>
		/// <param name="key">The key of the property to get or set.</param>
		/// <remarks>
		/// The getter will return JsonValue.Null if the given key is not assosiated with any value.
		/// </remarks>
		public JsonValue this[string key]
		{
			get
			{
				if (this.properties.TryGetValue(key, out var value))
				{
					return value;
				}
				else
				{
					var nullValue = JsonValue.Undefined;
					nullValue.path = this.path + "." + key;
					return nullValue;
				}
			}
			set
			{
				this.properties[key] = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of JsonObject.
		/// </summary>
		public JsonObject() : this(JsonOptions.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of JsonObject with the specified <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="options">Specifies the JsonOptions used to compare values.</param>
		public JsonObject(JsonOptions options)
		{
			this.path = "$";
			this.options = options;
			this.properties = new Dictionary<string, JsonValue>(options.PropertyNameComparer);
		}

		/// <summary>
		/// Initializes a new instance of JsonObject with the specified values and <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="options">Specifies the <see cref="JsonOptions"/> used to compare values.</param>
		/// <param name="values">A collection of key-value pairs to initialize the JsonObject.</param>
		public JsonObject(JsonOptions options, IEnumerable<KeyValuePair<string, JsonValue>> values)
		{
			this.path = "$";
			this.options = options;
			this.properties = new Dictionary<string, JsonValue>(values, options.PropertyNameComparer);
		}

		/// <summary>
		/// Returns an <see cref="JsonValue"/> representating this <see cref="JsonObject"/>.
		/// </summary>
		/// <returns></returns>
		public JsonValue AsJsonValue() => new JsonValue(this, this.options);

		/// <summary>
		/// Adds a value associated with a key to this collection only if the value is not null.
		/// </summary>
		/// <param name="key">The key of the property to be added.</param>
		/// <param name="value">The value of the property to be added.</param>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject AddIfNotNull(string key, JsonValue value)
		{
			if (!value.IsNull)
			{
				this.Add(key, value);
			}

			return this;
		}

		/// <summary>
		/// Retrieves a <see cref="JsonValue"/> from the JSON object by key, using the specified string comparison.
		/// </summary>
		/// <param name="key">The key to search for in the JSON object.</param>
		/// <param name="comparer">The <see cref="StringComparison"/> to use when comparing the key.</param>
		/// <returns>The <see cref="JsonValue"/> associated with the key, or <see cref="JsonValue.Null"/> if the key is not found.</returns>
		public JsonValue GetValue(string key, IEqualityComparer<string> comparer)
		{
			foreach (var item in this.properties)
			{
				if (comparer.Equals(item.Key, key))
				{
					return item.Value;
				}
			}
			return JsonValue.Null;
		}

		/// <inheritdoc/>
		public bool ContainsKey(string key)
		{
			return this.properties.ContainsKey(key);
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
		{
			return this.properties.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
		{
			return this.properties.Values.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
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
		/// Gets the properties of the current JSON object.
		/// </summary>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> containing the property names and their corresponding <see cref="JsonValue"/>.</returns>
		public IEnumerable<KeyValuePair<string, JsonValue>> GetProperties()
		{
			return this.properties;
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
			return JsonWriter.Serialize(this.AsJsonValue(), options);
		}

		/// <inheritdoc/>
		public void Add(string key, JsonValue value)
		{
			this.properties.Add(key, value);
		}

		/// <inheritdoc/>
		public bool TryGetValue(string key, [MaybeNullWhen(false)] out JsonValue value)
		{
			return this.properties.TryGetValue(key, out value);
		}

		/// <inheritdoc/>
		public void Add(KeyValuePair<string, JsonValue> item)
		{
			this.properties.Add(item);
		}

		/// <inheritdoc/>
		void ICollection<KeyValuePair<string, JsonValue>>.Clear()
		{
			this.properties.Clear();
		}

		/// <inheritdoc/>
		public bool Contains(KeyValuePair<string, JsonValue> item)
		{
			return this.properties.Contains(item);
		}

		/// <inheritdoc/>
		public void CopyTo(KeyValuePair<string, JsonValue>[] array, int arrayIndex)
		{
			this.properties.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc/>
		public bool Remove(KeyValuePair<string, JsonValue> item)
		{
			return this.properties.Remove(item);
		}

		/// <inheritdoc/>
		public bool Remove(string key)
		{
			return this.properties.Remove(key);
		}

		/// <exclude/>
		public static implicit operator HttpContent(JsonObject value) => new StringContent(value.ToString(), Encoding.UTF8, "application/json");
	}
}
