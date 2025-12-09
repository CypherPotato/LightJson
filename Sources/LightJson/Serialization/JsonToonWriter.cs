using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LightJson.Serialization;

#nullable enable

/// <summary>
/// Specifies the key folding behavior for TOON serialization.
/// </summary>
public enum ToonKeyFolding
{
	/// <summary>
	/// Key folding is disabled.
	/// </summary>
	Off,

	/// <summary>
	/// Keys are folded using dot notation when safe to do so.
	/// </summary>
	Safe
}

/// <summary>
/// Represents a writer that outputs JSON values in TOON (Token-Oriented Object Notation) format.
/// </summary>
public sealed class JsonToonWriter : IDisposable
{
	private readonly TextWriter _writer;
	private readonly HashSet<object> _renderingCollections;
	private bool _disposed;

	/// <summary>
	/// Gets or sets the number of spaces per indentation level. Default is 2.
	/// </summary>
	public int IndentSize { get; set; } = 2;

	/// <summary>
	/// Gets or sets the delimiter character used for separating values.
	/// Supported values: ',' (comma, default), '\t' (tab), '|' (pipe).
	/// </summary>
	public char Delimiter { get; set; } = ',';

	/// <summary>
	/// Gets or sets the newline string to use. Default is <see cref="Environment.NewLine"/>.
	/// </summary>
	public string NewLine { get; set; } = Environment.NewLine;

	/// <summary>
	/// Gets or sets the key folding mode. Default is <see cref="ToonKeyFolding.Off"/>.
	/// </summary>
	public ToonKeyFolding KeyFolding { get; set; } = ToonKeyFolding.Off;

	/// <summary>
	/// Gets or sets the maximum depth for key folding/flattening. Default is 2.
	/// </summary>
	public int FlattenDepth { get; set; } = 2;

	/// <summary>
	/// Initializes a new instance of <see cref="JsonToonWriter"/> with the specified <see cref="TextWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="TextWriter"/> to write TOON output to.</param>
	public JsonToonWriter(TextWriter writer)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_renderingCollections = new HashSet<object>(ReferenceEqualityComparer.Instance);
	}

	/// <summary>
	/// Writes the specified <see cref="JsonValue"/> to the output in TOON format.
	/// </summary>
	/// <param name="value">The <see cref="JsonValue"/> to write.</param>
	public void Write(JsonValue value)
	{
		switch (value.Type)
		{
			case JsonValueType.Object:
				Write(value.GetJsonObject());
				break;
			case JsonValueType.Array:
				Write(value.GetJsonArray());
				break;
			case JsonValueType.Null:
			case JsonValueType.Undefined:
			case JsonValueType.Boolean:
			case JsonValueType.Number:
			case JsonValueType.String:
				// Root primitive
				WritePrimitiveValue(value);
				break;
			default:
				throw new JsonSerializationException(JsonSerializationException.ErrorType.InvalidValueType);
		}
	}

	/// <summary>
	/// Writes the specified <see cref="JsonObject"/> to the output in TOON format.
	/// </summary>
	/// <param name="obj">The <see cref="JsonObject"/> to write.</param>
	public void Write(JsonObject obj)
	{
		if (obj == null) throw new ArgumentNullException(nameof(obj));

		AddRenderingCollection(obj);
		WriteObject(obj, 0);
		RemoveRenderingCollection(obj);
	}

	/// <summary>
	/// Writes the specified <see cref="JsonArray"/> to the output in TOON format.
	/// </summary>
	/// <param name="array">The <see cref="JsonArray"/> to write.</param>
	public void Write(JsonArray array)
	{
		if (array == null) throw new ArgumentNullException(nameof(array));

		AddRenderingCollection(array);
		WriteRootArray(array);
		RemoveRenderingCollection(array);
	}

	/// <summary>
	/// Serializes the specified object to TOON format.
	/// </summary>
	/// <param name="value">The object to serialize.</param>
	/// <param name="options">The <see cref="JsonOptions"/> to use for serialization.</param>
	/// <returns>A string containing the TOON representation of the object.</returns>
	public static string SerializeToon(object? value, JsonOptions options)
	{
		if (options == null) throw new ArgumentNullException(nameof(options));

		var jsonValue = options.Serialize(value);

		using var stringWriter = new StringWriter();
		using var toonWriter = new JsonToonWriter(stringWriter);
		toonWriter.Write(jsonValue);
		return stringWriter.ToString();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!_disposed)
		{
			_writer.Flush();
			_disposed = true;
		}
	}

	#region Private Methods

	private void WriteObject(JsonObject obj, int depth)
	{
		bool first = true;
		foreach (var (path, value) in GetFlattenedProperties(obj))
		{
			if (!first)
			{
				_writer.Write(NewLine);
			}
			first = false;

			WriteIndentation(depth);
			WriteKeyPath(path);

			// For arrays, the colon is part of the array header (e.g., key[N]:)
			// For other values, we need the colon after the key
			if (value.Type != JsonValueType.Array)
			{
				_writer.Write(':');
			}

			WriteObjectFieldValue(value, depth);
		}
	}

	private IEnumerable<(List<string> Path, JsonValue Value)> GetFlattenedProperties(JsonObject obj, int currentFoldDepth = 0)
	{
		foreach (var kvp in obj)
		{
			if (CanFold(kvp.Key, kvp.Value, currentFoldDepth))
			{
				var childObj = kvp.Value.GetJsonObject();
				// Add child object to rendering collections to detect cycles during flattening?
				// Or just rely on the fact that we will eventually write it?
				// Cycle detection usually happens during write.
				// If we have a cycle in the structure we are flattening, GetFlattenedProperties will stack overflow.
				// We should probably check cycles here too.

				if (_renderingCollections.Contains(childObj))
				{
					throw new JsonSerializationException(JsonSerializationException.ErrorType.CircularReference);
				}
				_renderingCollections.Add(childObj);

				foreach (var childKvp in GetFlattenedProperties(childObj, currentFoldDepth + 1))
				{
					var newPath = new List<string> { kvp.Key };
					newPath.AddRange(childKvp.Path);
					yield return (newPath, childKvp.Value);
				}

				_renderingCollections.Remove(childObj);
			}
			else
			{
				yield return (new List<string> { kvp.Key }, kvp.Value);
			}
		}
	}

	private bool CanFold(string key, JsonValue value, int currentFoldDepth)
	{
		if (KeyFolding != ToonKeyFolding.Safe) return false;
		if (currentFoldDepth >= FlattenDepth) return false;
		if (value.Type != JsonValueType.Object) return false;
		var obj = value.GetJsonObject();
		if (obj.Count == 0) return false;
		return true;
	}

	private void WriteKeyPath(List<string> path)
	{
		for (int i = 0; i < path.Count; i++)
		{
			WriteKey(path[i]);
			if (i < path.Count - 1)
			{
				_writer.Write('.');
			}
		}
	}

	private void WriteObjectFieldValue(JsonValue value, int depth, bool skipColonForArray = false)
	{
		switch (value.Type)
		{
			case JsonValueType.Object:
				var nestedObj = value.GetJsonObject();
				if (nestedObj.Count == 0)
				{
					// Empty object: just key: with nothing after
					// Actually per TOON spec: "key: alone for nested or empty objects"
				}
				else
				{
					AddRenderingCollection(nestedObj);
					_writer.Write(NewLine);
					WriteObject(nestedObj, depth + 1);
					RemoveRenderingCollection(nestedObj);
				}
				break;

			case JsonValueType.Array:
				var array = value.GetJsonArray();
				AddRenderingCollection(array);
				WriteArrayField(array, depth);
				RemoveRenderingCollection(array);
				break;

			default:
				// Primitive value
				_writer.Write(' ');
				WritePrimitiveValue(value);
				break;
		}
	}

	private void WriteArrayField(JsonArray array, int depth)
	{
		// Determine array form: primitive inline, tabular, or expanded list
		if (IsPrimitiveArray(array))
		{
			// Inline primitive array: key[N]: v1,v2,...
			WriteArrayHeader(null, array.Count);
			if (array.Count > 0)
			{
				_writer.Write(' ');
				WritePrimitiveArrayValues(array);
			}
		}
		else if (IsTabularArray(array, out var fieldNames))
		{
			// Tabular form: key[N]{f1,f2,...}:
			WriteTabularArrayField(array, fieldNames!, depth);
		}
		else if (IsArrayOfPrimitiveArrays(array))
		{
			// Arrays of arrays (primitives): expanded list
			WriteArrayOfPrimitiveArraysField(array, depth);
		}
		else
		{
			// Mixed/non-uniform: expanded list
			WriteExpandedListArrayField(array, depth);
		}
	}

	private void WriteRootArray(JsonArray array)
	{
		if (IsPrimitiveArray(array))
		{
			// Root inline primitive array: [N]: v1,v2,...
			WriteArrayHeader(null, array.Count);
			_writer.Write(' ');
			WritePrimitiveArrayValues(array);
		}
		else if (IsTabularArray(array, out var fieldNames))
		{
			// Root tabular: [N]{f1,f2,...}:
			WriteRootTabularArray(array, fieldNames!);
		}
		else if (IsArrayOfPrimitiveArrays(array))
		{
			// Root arrays of arrays (primitives)
			WriteRootArrayOfPrimitiveArrays(array);
		}
		else
		{
			// Root mixed/non-uniform
			WriteRootExpandedListArray(array);
		}
	}

	private bool IsPrimitiveArray(JsonArray array)
	{
		foreach (var item in array)
		{
			if (item.Type == JsonValueType.Array || item.Type == JsonValueType.Object)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsArrayOfPrimitiveArrays(JsonArray array)
	{
		if (array.Count == 0) return false;

		foreach (var item in array)
		{
			if (item.Type != JsonValueType.Array)
			{
				return false;
			}

			if (!IsPrimitiveArray(item.GetJsonArray()))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsTabularArray(JsonArray array, out IList<string>? fieldNames)
	{
		fieldNames = null;

		if (array.Count == 0) return false;

		// All elements must be objects
		HashSet<string>? commonKeys = null;
		IList<string>? firstKeys = null;

		foreach (var item in array)
		{
			if (item.Type != JsonValueType.Object)
			{
				return false;
			}

			var obj = item.GetJsonObject();

			// All values must be primitives
			foreach (var kvp in obj)
			{
				if (kvp.Value.Type == JsonValueType.Array || kvp.Value.Type == JsonValueType.Object)
				{
					return false;
				}
			}

			var keys = new HashSet<string>(obj.Keys);

			if (commonKeys == null)
			{
				commonKeys = keys;
				firstKeys = new List<string>(obj.Keys);
			}
			else
			{
				if (!commonKeys.SetEquals(keys))
				{
					return false;
				}
			}
		}

		fieldNames = firstKeys;
		return true;
	}

	private void WritePrimitiveArrayInline(JsonArray array)
	{
		WriteArrayHeader(null, array.Count);
		_writer.Write(' ');
		WritePrimitiveArrayValues(array);
	}

	private void WritePrimitiveArrayValues(JsonArray array)
	{
		bool first = true;
		foreach (var item in array)
		{
			if (!first)
			{
				_writer.Write(Delimiter);
			}
			first = false;
			WritePrimitiveValue(item);
		}
	}

	private void WriteTabularArrayField(JsonArray array, IList<string> fieldNames, int depth)
	{
		// Write header with fields on same line as key
		WriteTabularHeader(null, array.Count, fieldNames);
		_writer.Write(NewLine);

		// Write rows
		bool firstRow = true;
		foreach (var item in array)
		{
			if (!firstRow)
			{
				_writer.Write(NewLine);
			}
			firstRow = false;

			WriteIndentation(depth + 1);
			WriteTabularRow(item.GetJsonObject(), fieldNames);
		}
	}

	private void WriteRootTabularArray(JsonArray array, IList<string> fieldNames)
	{
		WriteTabularHeader(null, array.Count, fieldNames);
		_writer.Write(NewLine);

		bool firstRow = true;
		foreach (var item in array)
		{
			if (!firstRow)
			{
				_writer.Write(NewLine);
			}
			firstRow = false;

			WriteIndentation(1);
			WriteTabularRow(item.GetJsonObject(), fieldNames);
		}
	}

	private void WriteTabularRow(JsonObject obj, IList<string> fieldNames)
	{
		bool first = true;
		foreach (var fieldName in fieldNames)
		{
			if (!first)
			{
				_writer.Write(Delimiter);
			}
			first = false;

			WritePrimitiveValue(obj[fieldName]);
		}
	}

	private void WriteTabularHeader(string? key, int count, IList<string> fieldNames)
	{
		if (key != null)
		{
			WriteKey(key);
		}
		_writer.Write('[');
		_writer.Write(count.ToString(CultureInfo.InvariantCulture));
		WriteDelimiterSymbol();
		_writer.Write("]{");

		bool first = true;
		foreach (var fieldName in fieldNames)
		{
			if (!first)
			{
				_writer.Write(Delimiter);
			}
			first = false;
			WriteKey(fieldName);
		}
		_writer.Write("}:");
	}

	private void WriteArrayOfPrimitiveArraysField(JsonArray array, int depth)
	{
		// Parent header on same line as key: key[N]:
		WriteArrayHeader(null, array.Count);
		_writer.Write(NewLine);

		// Each inner array as list item
		bool firstItem = true;
		foreach (var item in array)
		{
			if (!firstItem)
			{
				_writer.Write(NewLine);
			}
			firstItem = false;

			var innerArray = item.GetJsonArray();
			WriteIndentation(depth);
			_writer.Write("- ");
			WriteArrayHeader(null, innerArray.Count);
			_writer.Write(' ');
			WritePrimitiveArrayValues(innerArray);
		}
	}

	private void WriteRootArrayOfPrimitiveArrays(JsonArray array)
	{
		WriteArrayHeader(null, array.Count);
		_writer.Write(NewLine);

		bool firstItem = true;
		foreach (var item in array)
		{
			if (!firstItem)
			{
				_writer.Write(NewLine);
			}
			firstItem = false;

			var innerArray = item.GetJsonArray();
			WriteIndentation(1);
			_writer.Write("- ");
			WriteArrayHeader(null, innerArray.Count);
			_writer.Write(' ');
			WritePrimitiveArrayValues(innerArray);
		}
	}

	private void WriteExpandedListArrayField(JsonArray array, int depth)
	{
		// Header on same line as key: key[N]:
		WriteArrayHeader(null, array.Count);
		_writer.Write(NewLine);

		// Each element as list item
		bool firstItem = true;
		foreach (var item in array)
		{
			if (!firstItem)
			{
				_writer.Write(NewLine);
			}
			firstItem = false;

			WriteListItem(item, depth);
		}
	}

	private void WriteRootExpandedListArray(JsonArray array)
	{
		WriteArrayHeader(null, array.Count);
		_writer.Write(NewLine);

		bool firstItem = true;
		foreach (var item in array)
		{
			if (!firstItem)
			{
				_writer.Write(NewLine);
			}
			firstItem = false;

			WriteListItem(item, 0);
		}
	}

	private void WriteListItem(JsonValue item, int depth)
	{
		switch (item.Type)
		{
			case JsonValueType.Object:
				var obj = item.GetJsonObject();
				if (obj.Count == 0)
				{
					// Empty object list item: just "-"
					WriteIndentation(depth);
					_writer.Write('-');
				}
				else
				{
					AddRenderingCollection(obj);
					WriteObjectAsListItem(obj, depth);
					RemoveRenderingCollection(obj);
				}
				break;

			case JsonValueType.Array:
				var arr = item.GetJsonArray();
				AddRenderingCollection(arr);

				WriteIndentation(depth);
				if (IsPrimitiveArray(arr))
				{
					// Inline primitive array in list item
					_writer.Write("- ");
					WriteArrayHeader(null, arr.Count);
					_writer.Write(' ');
					WritePrimitiveArrayValues(arr);
				}
				else
				{
					// Nested complex array - use expanded format
					_writer.Write("- ");
					WriteArrayHeader(null, arr.Count);
					_writer.Write(NewLine);

					bool first = true;
					foreach (var subItem in arr)
					{
						if (!first)
						{
							_writer.Write(NewLine);
						}
						first = false;
						WriteListItem(subItem, depth + 1);
					}
				}
				RemoveRenderingCollection(arr);
				break;

			default:
				// Primitive
				WriteIndentation(depth);
				_writer.Write("- ");
				WritePrimitiveValue(item);
				break;
		}
	}

	private void WriteObjectAsListItem(JsonObject obj, int depth)
	{
		// Per TOON spec Section 10:
		// When a list-item object has a tabular array as its first field,
		// the tabular header appears on the hyphen line.
		// Otherwise, first field on hyphen line.

		// Use flattened properties to handle folding
		var flattened = GetFlattenedProperties(obj).ToList();

		if (flattened.Count == 0)
		{
			WriteIndentation(depth);
			_writer.Write('-');
			return;
		}

		var (firstPath, firstValue) = flattened[0];

		// Check if first field is a tabular array
		if (firstValue.Type == JsonValueType.Array)
		{
			var firstArray = firstValue.GetJsonArray();
			if (IsTabularArray(firstArray, out var fieldNames))
			{
				// Tabular array as first field: - key[N]{fields}:
				WriteIndentation(depth);
				_writer.Write("- ");

				// Write path except last segment
				for (int i = 0; i < firstPath.Count - 1; i++)
				{
					WriteKey(firstPath[i]);
					_writer.Write('.');
				}

				// Write last segment with tabular header
				WriteKey(firstPath[firstPath.Count - 1]);
				_writer.Write('[');
				_writer.Write(firstArray.Count.ToString(CultureInfo.InvariantCulture));
				WriteDelimiterSymbol();
				_writer.Write("]{");

				bool firstField = true;
				foreach (var fn in fieldNames!)
				{
					if (!firstField)
					{
						_writer.Write(Delimiter);
					}
					firstField = false;
					WriteKey(fn);
				}
				_writer.Write("}:");
				_writer.Write(NewLine);

				// Write rows at depth + 2
				AddRenderingCollection(firstArray);
				bool firstRow = true;
				foreach (var rowItem in firstArray)
				{
					if (!firstRow)
					{
						_writer.Write(NewLine);
					}
					firstRow = false;
					WriteIndentation(depth + 2);
					WriteTabularRow(rowItem.GetJsonObject(), fieldNames);
				}
				RemoveRenderingCollection(firstArray);

				// Write remaining fields at depth + 1
				for (int i = 1; i < flattened.Count; i++)
				{
					var (path, value) = flattened[i];
					_writer.Write(NewLine);
					WriteIndentation(depth + 1);
					WriteKeyPath(path);
					if (value.Type != JsonValueType.Array)
					{
						_writer.Write(':');
					}
					WriteObjectFieldValue(value, depth + 1);
				}
				return;
			}
		}

		// Standard object as list item: first field on hyphen line
		WriteIndentation(depth);
		_writer.Write("- ");
		WriteKeyPath(firstPath);

		if (firstValue.Type == JsonValueType.Object)
		{
			_writer.Write(':');
			var nestedObj = firstValue.GetJsonObject();
			if (nestedObj.Count > 0)
			{
				AddRenderingCollection(nestedObj);
				_writer.Write(NewLine);
				// Nested fields at depth + 2 (per Section 10)
				WriteObject(nestedObj, depth + 2);
				RemoveRenderingCollection(nestedObj);
			}
		}
		else if (firstValue.Type == JsonValueType.Array)
		{
			var arr = firstValue.GetJsonArray();
			AddRenderingCollection(arr);
			WriteArrayFieldForListItem(arr, depth);
			RemoveRenderingCollection(arr);
		}
		else
		{
			// Primitive
			_writer.Write(':');
			_writer.Write(' ');
			WritePrimitiveValue(firstValue);
		}

		// Write remaining fields at depth + 1
		for (int i = 1; i < flattened.Count; i++)
		{
			var (path, value) = flattened[i];
			_writer.Write(NewLine);
			WriteIndentation(depth + 1);
			WriteKeyPath(path);
			if (value.Type != JsonValueType.Array)
			{
				_writer.Write(':');
			}
			WriteObjectFieldValue(value, depth + 1);
		}
	}

	private void WriteArrayFieldForListItem(JsonArray array, int depth)
	{
		if (IsPrimitiveArray(array))
		{
			WriteArrayHeader(null, array.Count);
			if (array.Count > 0)
			{
				_writer.Write(' ');
				WritePrimitiveArrayValues(array);
			}
		}
		else if (IsTabularArray(array, out var fieldNames))
		{
			// Tabular array: key[N]{fields}: on same line
			WriteTabularHeader(null, array.Count, fieldNames!);
			_writer.Write(NewLine);

			bool firstRow = true;
			foreach (var item in array)
			{
				if (!firstRow)
				{
					_writer.Write(NewLine);
				}
				firstRow = false;
				WriteIndentation(depth + 2);
				WriteTabularRow(item.GetJsonObject(), fieldNames!);
			}
		}
		else if (IsArrayOfPrimitiveArrays(array))
		{
			WriteArrayHeader(null, array.Count);
			_writer.Write(NewLine);

			bool firstItem = true;
			foreach (var item in array)
			{
				if (!firstItem)
				{
					_writer.Write(NewLine);
				}
				firstItem = false;

				var innerArray = item.GetJsonArray();
				WriteIndentation(depth + 1);
				_writer.Write("- ");
				WriteArrayHeader(null, innerArray.Count);
				_writer.Write(' ');
				WritePrimitiveArrayValues(innerArray);
			}
		}
		else
		{
			WriteArrayHeader(null, array.Count);
			_writer.Write(NewLine);

			bool firstItem = true;
			foreach (var item in array)
			{
				if (!firstItem)
				{
					_writer.Write(NewLine);
				}
				firstItem = false;
				WriteListItem(item, depth + 1);
			}
		}
	}

	private void WriteArrayHeader(string? key, int count)
	{
		if (key != null)
		{
			WriteKey(key);
		}
		_writer.Write('[');
		_writer.Write(count.ToString(CultureInfo.InvariantCulture));
		WriteDelimiterSymbol();
		_writer.Write("]:");
	}

	private void WriteDelimiterSymbol()
	{
		// Per TOON spec: delimiter symbol in brackets
		// comma = absent, tab = HTAB, pipe = |
		if (Delimiter == '\t')
		{
			_writer.Write('\t');
		}
		else if (Delimiter == '|')
		{
			_writer.Write('|');
		}
		// comma: no symbol written
	}

	private void WriteIndentation(int depth)
	{
		for (int i = 0; i < depth * IndentSize; i++)
		{
			_writer.Write(' ');
		}
	}

	private void WriteKey(string key)
	{
		// Per TOON spec Section 7.3:
		// Keys MAY be unquoted only if they match: ^[A-Za-z_][A-Za-z0-9_.]*$
		if (IsValidUnquotedKey(key))
		{
			_writer.Write(key);
		}
		else
		{
			WriteQuotedString(key);
		}
	}

	private static readonly Regex UnquotedKeyPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled);

	private static bool IsValidUnquotedKey(string key)
	{
		if (string.IsNullOrEmpty(key)) return false;
		return UnquotedKeyPattern.IsMatch(key);
	}

	private void WritePrimitiveValue(JsonValue value)
	{
		switch (value.Type)
		{
			case JsonValueType.Null:
			case JsonValueType.Undefined:
				_writer.Write("null");
				break;

			case JsonValueType.Boolean:
				_writer.Write(value.GetBoolean() ? "true" : "false");
				break;

			case JsonValueType.Number:
				WriteNumber(value.GetNumber());
				break;

			case JsonValueType.String:
				WriteStringValue(value.GetString());
				break;

			default:
				throw new JsonSerializationException(JsonSerializationException.ErrorType.InvalidValueType);
		}
	}

	private void WriteNumber(double number)
	{
		// Per TOON spec Section 2:
		// - No exponent notation
		// - No trailing zeros in fractional part
		// - -0 MUST be normalized to 0
		// - NaN, +Infinity, -Infinity → null

		if (double.IsNaN(number) || double.IsInfinity(number))
		{
			_writer.Write("null");
			return;
		}

		// Handle -0
		if (number == 0 && double.IsNegative(number))
		{
			number = 0;
		}

		// Use decimal for better precision and to avoid scientific notation
		decimal decValue = (decimal)number;
		string result = decValue.ToString(CultureInfo.InvariantCulture);

		// Ensure no exponent notation (decimal.ToString shouldn't produce it, but verify)
		if (result.Contains('E') || result.Contains('e'))
		{
			// Fallback: format without exponent
			result = decValue.ToString("F15", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
		}

		_writer.Write(result);
	}

	private void WriteStringValue(string value)
	{
		// Per TOON spec Section 7.2: Quoting Rules
		if (MustQuoteString(value))
		{
			WriteQuotedString(value);
		}
		else
		{
			_writer.Write(value);
		}
	}

	private bool MustQuoteString(string value)
	{
		// Per TOON spec Section 7.2, a string MUST be quoted if:
		// - It is empty ("")
		if (string.IsNullOrEmpty(value)) return true;

		// - It has leading or trailing whitespace
		if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])) return true;

		// - It equals true, false, or null (case-sensitive)
		if (value == "true" || value == "false" || value == "null") return true;

		// - It is numeric-like
		if (IsNumericLike(value)) return true;

		// - It contains a colon (:), double quote ("), or backslash (\)
		if (value.Contains(':') || value.Contains('"') || value.Contains('\\')) return true;

		// - It contains brackets or braces ([, ], {, })
		if (value.Contains('[') || value.Contains(']') || value.Contains('{') || value.Contains('}')) return true;

		// - It contains control characters: newline, carriage return, or tab
		if (value.Contains('\n') || value.Contains('\r') || value.Contains('\t')) return true;

		// - It contains the active delimiter
		if (value.Contains(Delimiter)) return true;

		// - It equals "-" or starts with "-"
		if (value.StartsWith('-')) return true;

		return false;
	}

	private static readonly Regex NumericPattern = new Regex(@"^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex LeadingZeroPattern = new Regex(@"^0\d+$", RegexOptions.Compiled);

	private static bool IsNumericLike(string value)
	{
		// Matches /^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$/i
		if (NumericPattern.IsMatch(value)) return true;

		// Or matches /^0\d+$/ (leading-zero decimals)
		if (LeadingZeroPattern.IsMatch(value)) return true;

		return false;
	}

	private void WriteQuotedString(string value)
	{
		// Per TOON spec Section 7.1: Escaping
		// Only: \\ \" \n \r \t
		_writer.Write('"');

		foreach (char c in value)
		{
			switch (c)
			{
				case '\\':
					_writer.Write("\\\\");
					break;
				case '"':
					_writer.Write("\\\"");
					break;
				case '\n':
					_writer.Write("\\n");
					break;
				case '\r':
					_writer.Write("\\r");
					break;
				case '\t':
					_writer.Write("\\t");
					break;
				default:
					_writer.Write(c);
					break;
			}
		}

		_writer.Write('"');
	}

	private void AddRenderingCollection(object value)
	{
		if (!_renderingCollections.Add(value))
		{
			throw new JsonSerializationException(JsonSerializationException.ErrorType.CircularReference);
		}
	}

	private void RemoveRenderingCollection(object value)
	{
		_renderingCollections.Remove(value);
	}

	#endregion

	/// <summary>
	/// Reference equality comparer for collection circular reference detection.
	/// </summary>
	private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
		public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
	}
}
