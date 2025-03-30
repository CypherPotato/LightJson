using System.Collections;
using System.Collections.Generic;
using LightJson.Converters;

namespace LightJson;

/// <summary>
/// Represents an collection of <see cref="JsonConverter"/>.
/// </summary>
public sealed class JsonConverterCollection : IList<JsonConverter> {
	private readonly List<JsonConverter> _converters;

	/// <summary>
	/// Creates an new <see cref="JsonConverterCollection"/> with the specified
	/// <see cref="JsonOptions"/> object.
	/// </summary>
	public JsonConverterCollection () {
		this._converters = new List<JsonConverter> ();
	}

	/// <inheritdoc/>
	public JsonConverter this [ int index ] {
		get => this._converters [ index ];
		set => this._converters [ index ] = value;
	}

	/// <inheritdoc/>
	public int Count => this._converters.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public void Add ( JsonConverter item ) {
		this._converters.Add ( item );
	}

	/// <summary>
	/// Adds all items to the collection.
	/// </summary>
	/// <param name="items">The objects to add to the collection.</param>
	public void AddRange ( IEnumerable<JsonConverter> items ) {
		foreach (var item in items)
			this.Add ( item );
	}

	/// <inheritdoc/>
	public void Clear () {
		this._converters.Clear ();
	}

	/// <inheritdoc/>
	public bool Contains ( JsonConverter item ) {
		return this._converters.Contains ( item );
	}

	/// <inheritdoc/>
	public void CopyTo ( JsonConverter [] array, int arrayIndex ) {
		this._converters.CopyTo ( array, arrayIndex );
	}

	/// <inheritdoc/>
	public IEnumerator<JsonConverter> GetEnumerator () {
		return this._converters.GetEnumerator ();
	}

	/// <inheritdoc/>
	public int IndexOf ( JsonConverter item ) {
		return this._converters.IndexOf ( item );
	}

	/// <inheritdoc/>
	public void Insert ( int index, JsonConverter item ) {
		this._converters.Insert ( index, item );
	}

	/// <inheritdoc/>
	public bool Remove ( JsonConverter item ) {
		return this._converters.Remove ( item );
	}

	/// <inheritdoc/>
	public void RemoveAt ( int index ) {
		this._converters.RemoveAt ( index );
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator () {
		return this.GetEnumerator ();
	}
}
