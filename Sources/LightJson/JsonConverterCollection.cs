using LightJson.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson;

/// <summary>
/// Represents an collection of <see cref="JsonConverter"/>.
/// </summary>
public class JsonConverterCollection : ICollection<JsonConverter>
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
	public int Count => ((ICollection<JsonConverter>)_converters).Count;

	/// <inheritdoc/>
	public bool IsReadOnly => ((ICollection<JsonConverter>)_converters).IsReadOnly;

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
		((ICollection<JsonConverter>)_converters).Clear();
	}

	/// <inheritdoc/>
	public bool Contains(JsonConverter item)
	{
		return ((ICollection<JsonConverter>)_converters).Contains(item);
	}

	/// <inheritdoc/>
	public void CopyTo(JsonConverter[] array, int arrayIndex)
	{
		((ICollection<JsonConverter>)_converters).CopyTo(array, arrayIndex);
	}

	/// <inheritdoc/>
	public IEnumerator<JsonConverter> GetEnumerator()
	{
		return ((IEnumerable<JsonConverter>)_converters).GetEnumerator();
	}

	/// <inheritdoc/>
	public bool Remove(JsonConverter item)
	{
		return ((ICollection<JsonConverter>)_converters).Remove(item);
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_converters).GetEnumerator();
	}
}
