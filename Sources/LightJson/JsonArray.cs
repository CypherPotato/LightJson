using LightJson.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightJson
{
	/// <summary>
	/// Represents an ordered collection of JsonValues.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public sealed class JsonArray : IEnumerable<JsonValue>, IList<JsonValue>, IImplicitJsonValue
	{
		internal string path = "";
		private IList<JsonValue> items;
		private JsonOptions options;

		/// <inheritdoc/>
		public int Count
		{
			get
			{
				return this.items.Count;
			}
		}

		/// <inheritdoc/>
		public bool IsReadOnly => items.IsReadOnly;

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
		/// Returns an <see cref="JsonValue"/> representating this <see cref="JsonArray"/>.
		/// </summary>
		/// <returns></returns>
		public JsonValue AsJsonValue() => new JsonValue(this, options);

		/// <inheritdoc/>
		public IEnumerator<JsonValue> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)items).GetEnumerator();
		}

		/// <inheritdoc/>
		public int IndexOf(JsonValue item)
		{
			return items.IndexOf(item);
		}

		/// <inheritdoc/>
		public void Insert(int index, JsonValue item)
		{
			items.Insert(index, item);
		}

		/// <inheritdoc/>
		public void RemoveAt(int index)
		{
			items.RemoveAt(index);
		}

		/// <inheritdoc/>
		public void Add(JsonValue item)
		{
			items.Add(item);
		}

		/// <inheritdoc/>
		public void Clear()
		{
			items.Clear();
		}

		/// <inheritdoc/>
		public bool Contains(JsonValue item)
		{
			return items.Contains(item);
		}

		/// <inheritdoc/>
		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			items.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc/>
		public bool Remove(JsonValue item)
		{
			return items.Remove(item);
		}


		/// <summary>
		/// Returns a JSON string representing the state of this value.
		/// </summary>
		public override string ToString()
		{
			return ToString(JsonOptions.Default);
		}

		/// <inheritdoc />
		public string ToString(JsonOptions options)
		{
			return JsonWriter.Serialize(this.AsJsonValue(), options);
		}
	}
}
