using LightJson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightJson
{
	/// <summary>
	/// Represents an ordered collection of JsonValues.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(JsonArrayDebugView))]
	public sealed class JsonArray : IEnumerable<JsonValue>
	{
		internal string path = "";
		private IList<JsonValue> items;
		private JsonOptions options;

		/// <summary>
		/// Gets the number of values in this collection.
		/// </summary>
		public int Count
		{
			get
			{
				return this.items.Count;
			}
		}

		/// <summary>
		/// Gets or sets the value at the given index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <remarks>
		/// The getter will return JsonValue.Null if the given index is out of range.
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
			this.items = new List<JsonValue>();
			this.options = options;
		}

		/// <summary>
		/// Returns an <see cref="JsonValue"/> representating this <see cref="JsonObject"/>.
		/// </summary>
		/// <returns></returns>
		public JsonValue AsJsonValue() => new JsonValue(this, options);

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
		/// Adds the given value to this collection.
		/// </summary>
		/// <param name="value">The value to be added.</param>
		/// <returns>Returns this collection.</returns>
		public JsonArray Add(JsonValue value)
		{
			this.items.Add(value);
			return this;
		}

		/// <summary>
		/// Adds the given value to this collection only if the value is not null.
		/// </summary>
		/// <param name="value">The value to be added.</param>
		/// <returns>Returns this collection.</returns>
		public JsonArray AddIfNotNull(JsonValue value)
		{
			if (!value.IsNull)
			{
				Add(value);
			}

			return this;
		}

		/// <summary>
		/// Inserts the given value at the given index in this collection.
		/// </summary>
		/// <param name="index">The index where the given value will be inserted.</param>
		/// <param name="value">The value to be inserted into this collection.</param>
		/// <returns>Returns this collection.</returns>
		public JsonArray Insert(int index, JsonValue value)
		{
			this.items.Insert(index, value);
			return this;
		}

		/// <summary>
		/// Inserts the given value at the given index in this collection.
		/// </summary>
		/// <param name="index">The index where the given value will be inserted.</param>
		/// <param name="value">The value to be inserted into this collection.</param>
		/// <returns>Returns this collection.</returns>
		public JsonArray InsertIfNotNull(int index, JsonValue value)
		{
			if (!value.IsNull)
			{
				Insert(index, value);
			}

			return this;
		}

		/// <summary>
		/// Removes the value at the given index.
		/// </summary>
		/// <param name="index">The index of the value to be removed.</param>
		/// <returns>Return this collection.</returns>
		public JsonArray Remove(int index)
		{
			this.items.RemoveAt(index);
			return this;
		}

		/// <summary>
		/// Clears the contents of this collection.
		/// </summary>
		/// <returns>Returns this collection.</returns>
		public JsonArray Clear()
		{
			this.items.Clear();
			return this;
		}

		/// <summary>
		/// Determines whether the given item is in the JsonArray.
		/// </summary>
		/// <param name="item">The item to locate in the JsonArray.</param>
		/// <returns>Returns true if the item is found; otherwise, false.</returns>
		public bool Contains(JsonValue item)
		{
			return this.items.Contains(item);
		}

		/// <summary>
		/// Determines the index of the given item in this JsonArray.
		/// </summary>
		/// <param name="item">The item to locate in this JsonArray.</param>
		/// <returns>The index of the item, if found. Otherwise, returns -1.</returns>
		public int IndexOf(JsonValue item)
		{
			return this.items.IndexOf(item);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		public IEnumerator<JsonValue> GetEnumerator()
		{
			return this.items.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Returns a JSON string representing the state of the array.
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
		/// Returns a JSON string representing the state of the array.
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

		private class JsonArrayDebugView
		{
			private JsonArray jsonArray;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public JsonValue[] Items
			{
				get
				{
					var items = new JsonValue[this.jsonArray.Count];

					for (int i = 0; i < this.jsonArray.Count; i += 1)
					{
						items[i] = this.jsonArray[i];
					}

					return items;
				}
			}

			public JsonArrayDebugView(JsonArray jsonArray)
			{
				this.jsonArray = jsonArray;
			}
		}
	}
}
