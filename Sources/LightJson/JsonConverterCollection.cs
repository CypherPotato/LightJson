using LightJson.Converters;
using System.Collections;
using System.Collections.Generic;

namespace LightJson;

/// <summary>
/// Represents an collection of <see cref="JsonConverter"/>.
/// </summary>
public class JsonConverterCollection : IList<JsonConverter>
{
	private List<JsonConverter> _converters;
	private JsonOptions parent;

	/// <summary>
	/// Creates an new <see cref="JsonConverterCollection"/> with the specified
	/// <see cref="JsonOptions"/> object.
	/// </summary>
	/// <param name="parent">The parent JsonOptions object.</param>
	public JsonConverterCollection(JsonOptions parent)
	{
		_converters = new List<JsonConverter>();
		this.parent = parent;
	}

	/// <inheritdoc/>
	public JsonConverter this[int index]
	{
		get => _converters[index];
		set => _converters[index] = value;
	}

	/// <inheritdoc/>
	public int Count => _converters.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public void Add(JsonConverter item)
	{
		item.CurrentOptions = parent;
		_converters.Add(item);
	}

	/// <summary>
	/// Adds all items to the collection.
	/// </summary>
	/// <param name="items">The objects to add to the collection.</param>
	public void AddRange(IEnumerable<JsonConverter> items)
	{
		foreach (var item in items)
			Add(item);
	}

	/// <inheritdoc/>
	public void Clear()
	{
		_converters.Clear();
	}

	/// <inheritdoc/>
	public bool Contains(JsonConverter item)
	{
		return _converters.Contains(item);
	}

	/// <inheritdoc/>
	public void CopyTo(JsonConverter[] array, int arrayIndex)
	{
		_converters.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc/>
	public IEnumerator<JsonConverter> GetEnumerator()
	{
		return _converters.GetEnumerator();
	}

	/// <inheritdoc/>
	public int IndexOf(JsonConverter item)
	{
		return _converters.IndexOf(item);
	}

	/// <inheritdoc/>
	public void Insert(int index, JsonConverter item)
	{
		_converters.Insert(index, item);
	}

	/// <inheritdoc/>
	public bool Remove(JsonConverter item)
	{
		return _converters.Remove(item);
	}

	/// <inheritdoc/>
	public void RemoveAt(int index)
	{
		_converters.RemoveAt(index);
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
