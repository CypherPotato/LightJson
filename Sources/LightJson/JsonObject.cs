using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using LightJson.Serialization;

namespace LightJson {
	/// <summary>
	/// Represents a key-value pair collection of <see cref="JsonValue"/> objects.
	/// </summary>
	[DebuggerDisplay ( "Count = {Count}" )]
	[JsonConverter ( typeof ( JsonObjectInternalConverter ) )]
	public sealed class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>, IDictionary<string, JsonValue>, IImplicitJsonValue, IEquatable<JsonObject>, IEquatable<JsonValue> {
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
		public int Count {
			get {
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
		/// Gets or sets a value indicating whether to preserve the exact property names on this object properties.
		/// Setting this property doens't affect nested JSON object from this object.
		/// </summary>
		public bool PreserveExactNamingConvention { get; set; }

		/// <summary>
		/// Gets or sets the property with the given key.
		/// </summary>
		/// <param name="key">The key of the property to get or set.</param>
		/// <remarks>
		/// The getter will return JsonValue.Null if the given key is not assosiated with any value.
		/// </remarks>
		public JsonValue this [ string key ] {
			get {
				if (this.properties.TryGetValue ( key, out var value )) {
					return value;
				}
				else {
					var nullValue = new JsonValue ( JsonValueType.Undefined, default, default, this.options ) {
						path = this.path + "." + key
					};
					return nullValue;
				}
			}
			set {
				if (value.Type == JsonValueType.Undefined) {
					this.Remove ( key );
				}
				else {
					this.properties [ key ] = value;
				}
			}
		}

		/// <summary>
		/// Recursively merges an array of <see cref="JsonObject"/> instances into a single
		/// <see cref="JsonObject"/>. The merge is performed left‑to‑right, where each subsequent
		/// <see cref="JsonObject"/> overwrites or merges into the accumulated result.
		/// </summary>
		/// <param name="items">
		/// The <see cref="JsonObject"/> instances to merge. The array must contain at least one
		/// element; otherwise an <see cref="ArgumentOutOfRangeException"/> is thrown.
		/// </param>
		/// <returns>
		/// A new <see cref="JsonObject"/> that contains the merged contents of all supplied
		/// <paramref name="items"/>.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="items"/> is empty.
		/// </exception>
		public static JsonObject RecursiveMerge ( params JsonObject [] items ) {
			ArgumentOutOfRangeException.ThrowIfZero ( items.Length, nameof ( items ) );

			var carry = items [ 0 ];
			for (var i = 1; i < items.Length; i++) {
				carry = RecursiveMerge ( carry, items [ i ] );
			}

			return carry;
		}

		/// <summary>
		/// Recursively merges two <see cref="JsonObject"/> instances. For each property in
		/// <paramref name="b"/>, if the same key exists in <paramref name="a"/> and both values are
		/// objects (<see cref="JsonValueType.Object"/>), the merge proceeds recursively; otherwise
		/// the value from <paramref name="b"/> overwrites the value in <paramref name="a"/>.
		/// </summary>
		/// <param name="a">
		/// The base <see cref="JsonObject"/>. Its options are used to create the result instance.
		/// </param>
		/// <param name="b">
		/// The <see cref="JsonObject"/> whose properties are merged into <paramref name="a"/>.
		/// </param>
		/// <returns>
		/// A new <see cref="JsonObject"/> that contains the merged properties of <paramref name="a"/>
		/// and <paramref name="b"/>.
		/// </returns>
		public static JsonObject RecursiveMerge ( JsonObject a, JsonObject b ) {
			return RecursiveMerge ( a, b, 0, new HashSet<ObjectPair> () );
		}

		private static JsonObject RecursiveMerge ( JsonObject a, JsonObject b, int depth, HashSet<ObjectPair> visited ) {
			const int MaxDepth = 512;
			if (depth > MaxDepth) {
				throw new InvalidOperationException ( "Maximum recursion depth reached while merging JsonObject instances." );
			}

			if (!visited.Add ( new ObjectPair ( a, b ) )) {
				return a;
			}

			var result = new JsonObject ( a.options, a.properties );
			foreach (var (key, bValue) in b.properties) {
				if (result.properties.TryGetValue ( key, out var aValue )) {
					if (aValue.Type == JsonValueType.Object && bValue.Type == JsonValueType.Object) {
						result.properties [ key ] = RecursiveMerge ( aValue.GetJsonObject (), bValue.GetJsonObject (), depth + 1, visited );
					}
					else if (aValue.Type == JsonValueType.Array && bValue.Type == JsonValueType.Array) {
						var merged = JsonArray.Create ( aValue.GetJsonArray (), aValue.options );
						foreach (var item in bValue.GetJsonArray ()) {
							merged.Add ( item );
						}
						result.properties [ key ] = merged;
					}
					else {
						result.properties [ key ] = bValue;
					}
				}
				else {
					result.properties [ key ] = bValue;
				}
			}
			return result;
		}

		private readonly struct ObjectPair {
			private readonly JsonObject left;
			private readonly JsonObject right;

			public ObjectPair ( JsonObject left, JsonObject right ) {
				this.left = left;
				this.right = right;
			}

			public override bool Equals ( object obj ) {
				return obj is ObjectPair other
					&& ReferenceEquals ( this.left, other.left )
					&& ReferenceEquals ( this.right, other.right );
			}

			public override int GetHashCode () {
				return HashCode.Combine ( RuntimeHelpers.GetHashCode ( this.left ), RuntimeHelpers.GetHashCode ( this.right ) );
			}
		}

		/// <summary>
		/// Initializes a new instance of JsonObject.
		/// </summary>
		public JsonObject () : this ( JsonOptions.Default ) {
		}

		/// <summary>
		/// Initializes a new instance of JsonObject with the specified <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="options">Specifies the JsonOptions used to compare values.</param>
		public JsonObject ( JsonOptions options ) {
			this.path = "$";
			this.options = options;
			this.properties = new Dictionary<string, JsonValue> ( options.PropertyNameComparer );
		}

		/// <summary>
		/// Initializes a new instance of JsonObject with the specified values and <see cref="JsonOptions"/>.
		/// </summary>
		/// <param name="options">Specifies the <see cref="JsonOptions"/> used to compare values.</param>
		/// <param name="values">A collection of key-value pairs to initialize the JsonObject.</param>
		public JsonObject ( JsonOptions options, IEnumerable<KeyValuePair<string, JsonValue>> values ) {
			this.path = "$";
			this.options = options;
			this.properties = new Dictionary<string, JsonValue> ( values, options.PropertyNameComparer );
		}

		/// <summary>
		/// Returns an <see cref="JsonValue"/> representating this <see cref="JsonObject"/>.
		/// </summary>
		/// <returns></returns>
		public JsonValue AsJsonValue () => new JsonValue ( this, this.options );

		/// <summary>
		/// Adds a value associated with a key to this collection only if the value is not null.
		/// </summary>
		/// <param name="key">The key of the property to be added.</param>
		/// <param name="value">The value of the property to be added.</param>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject AddIfNotNull ( string key, JsonValue value ) {
			if (!value.IsNull) {
				this.Add ( key, value );
			}

			return this;
		}

		/// <summary>
		/// Retrieves a <see cref="JsonValue"/> from the JSON object by key, using the specified string comparison.
		/// </summary>
		/// <param name="key">The key to search for in the JSON object.</param>
		/// <param name="comparer">The <see cref="StringComparison"/> to use when comparing the key.</param>
		/// <returns>The <see cref="JsonValue"/> associated with the key, or <see cref="JsonValue.Undefined"/> if the key is not found.</returns>
		public JsonValue GetValue ( string key, IEqualityComparer<string> comparer ) {
			foreach (var item in this.properties) {
				if (comparer.Equals ( item.Key, key )) {
					return item.Value;
				}
			}
			return JsonValue.Undefined;
		}

		/// <summary>
		/// Retrieves a <see cref="JsonValue"/> from the JSON object by key using the <see cref="JsonOptions"/> property name comparer.
		/// </summary>
		/// <param name="key">The key to search for in the JSON object.</param>
		/// <returns>The <see cref="JsonValue"/> associated with the key, or <see cref="JsonValue.Undefined"/> if the key is not found.</returns>
		public JsonValue GetValue ( string key ) {
			return this.GetValue ( key, this.options.PropertyNameComparer );
		}

		/// <inheritdoc/>
		public bool ContainsKey ( string key ) {
			return this.properties.ContainsKey ( key );
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator () {
			return this.properties.GetEnumerator ();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator () {
			return this.properties.Values.GetEnumerator ();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
			return this.GetEnumerator ();
		}

		/// <summary>
		/// Returns a JSON string representing the state of the object.
		/// </summary>
		/// <remarks>
		/// The resulting string is safe to be inserted as is into dynamically
		/// generated JavaScript or JSON code.
		/// </remarks>
		public override string ToString () {
			return this.ToString ( this.options );
		}

		/// <summary>
		/// Gets the properties of the current JSON object.
		/// </summary>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> containing the property names and their corresponding <see cref="JsonValue"/>.</returns>
		public IEnumerable<KeyValuePair<string, JsonValue>> GetProperties () {
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
		public string ToString ( JsonOptions options ) {
			return JsonWriter.Serialize ( this.AsJsonValue (), options );
		}

		/// <summary>
		/// Adds a key-value pair to the collection, converting the value to an implicit JSON value.
		/// </summary>
		/// <typeparam name="TValue">The type of the value, which must implement <see cref="IImplicitJsonValue"/>.</typeparam>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add. This value will be converted to its JSON representation.</param>
		/// <returns>The original value that was added.</returns>
		public TValue Add<TValue> ( string key, TValue value ) where TValue : IImplicitJsonValue {
			if (properties.TryGetValue ( key, out var existingValue ) && existingValue.Equals ( value.AsJsonValue () )) {
				return value;
			}
			this.properties.Add ( key, value.AsJsonValue () );
			return value;
		}

		/// <inheritdoc/>
		public void Add ( string key, JsonValue value ) {
			this.properties.Add ( key, value );
		}

		/// <inheritdoc/>
		public bool TryGetValue ( string key, [MaybeNullWhen ( false )] out JsonValue value ) {
			return this.properties.TryGetValue ( key, out value );
		}

		/// <inheritdoc/>
		public void Add ( KeyValuePair<string, JsonValue> item ) {
			this.properties.Add ( item );
		}

		/// <inheritdoc/>
		void ICollection<KeyValuePair<string, JsonValue>>.Clear () {
			this.properties.Clear ();
		}

		/// <inheritdoc/>
		public bool Contains ( KeyValuePair<string, JsonValue> item ) {
			return this.properties.Contains ( item );
		}

		/// <inheritdoc/>
		public void CopyTo ( KeyValuePair<string, JsonValue> [] array, int arrayIndex ) {
			this.properties.CopyTo ( array, arrayIndex );
		}

		/// <inheritdoc/>
		public bool Remove ( KeyValuePair<string, JsonValue> item ) {
			return this.properties.Remove ( item );
		}

		/// <inheritdoc/>
		public bool Remove ( string key ) {
			return this.properties.Remove ( key );
		}

		/// <summary>
		/// Renames the key of an existing property.
		/// </summary>
		/// <param name="key">The current key to rename.</param>
		/// <param name="newKey">The new key name.</param>
		/// <returns><see langword="true"/> if the key was found and renamed; otherwise, <see langword="false"/>.</returns>
		public bool Rename ( string key, string newKey ) {
			if (this.properties.TryGetValue ( key, out var value )) {
				this.properties.Remove ( key );
				this.properties [ newKey ] = value;
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public bool Equals ( JsonObject? other ) {
			if (other is null)
				return false;

			return this.properties
				.ToArray ()
				.OrderBy ( s => s.Key )
				.SequenceEqual (
					other.properties.OrderBy ( s => s.Key ).ToArray () );
		}

		/// <inheritdoc/>
		public bool Equals ( JsonValue other ) {
			return other.Type == JsonValueType.Object && this.Equals ( other.GetJsonObject () );
		}

		/// <inheritdoc/>
		public override bool Equals ( object? obj ) {
			if (obj is JsonObject jobj) {
				return this.Equals ( jobj );
			}
			else if (obj is JsonValue jval) {
				return this.Equals ( jval );
			}

			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode () {
			int carry = UInt16.MaxValue;
			foreach (var item in this.properties) {
				carry ^= HashCode.Combine ( item.Key, item.Value );
			}
			return carry;
		}

		/// <exclude/>
		public static implicit operator HttpContent ( JsonObject value ) => new StringContent ( value.ToString (), Encoding.UTF8, "application/json" );
	}
}
