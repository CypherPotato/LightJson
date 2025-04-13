using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LightJson;

/// <summary>
/// Provides an string comparer for comparing similar JSON values, which are case-insensitive and only
/// considers alpha-numeric characters between the strings.
/// </summary>
public sealed class JsonSanitizedComparer : StringComparer
{
	/// <summary>
	/// Gets the singleton instance of the <see cref="JsonSanitizedComparer"/> class.
	/// </summary>
	public static JsonSanitizedComparer Instance { get; } = new JsonSanitizedComparer();

	/// <summary>
	/// Creates an new instance of the <see cref="JsonSanitizedComparer"/> class.
	/// </summary>
	public JsonSanitizedComparer()
	{
	}

	[return: NotNullIfNotNull(nameof(value))]
	string? Sanitize(string? value)
	{
		if (value is null)
			return null;

		StringBuilder sb = new StringBuilder(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsLetterOrDigit(c))
			{
				_ = sb.Append(c);
			}
		}

		return sb.ToString().ToLowerInvariant();
	}

	/// <inheritdoc/>
	public override int Compare(string? x, string? y)
	{
		if (x == null && y == null) return 0;
		if (x == null) return -1;
		if (y == null) return 1;

		return string.Compare(this.Sanitize(x), this.Sanitize(y), StringComparison.OrdinalIgnoreCase);
	}

	/// <inheritdoc/>
	public override bool Equals(string? x, string? y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;

		return this.Compare(x, y) == 0;
	}

	/// <inheritdoc/>
	public override int GetHashCode(string obj)
	{
		if (obj == null)
		{
			return 0;
		}

		return this.Sanitize(obj).GetHashCode();
	}
}
