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
	/// Creates an new instance of the <see cref="JsonSanitizedComparer"/> class.
	/// </summary>
	public JsonSanitizedComparer()
	{
	}

	[return: NotNullIfNotNull(nameof(value))]
	string? Sanitize(string? value)
	{
		if (value is null) return null;

		StringBuilder sb = new StringBuilder(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsLetterOrDigit(c))
			{
				sb.Append(c);
			}
		}

		return sb.ToString().ToLowerInvariant();
	}

	/// <inheritdoc/>
	public override int Compare(string? x, string? y)
	{
		return string.Compare(this.Sanitize(x), this.Sanitize(y), true);
	}

	/// <inheritdoc/>
	public override bool Equals(string? x, string? y)
	{
		return this.Compare(x, y) == 0;
	}

	/// <inheritdoc/>
	public override int GetHashCode(string obj)
	{
		return this.Sanitize(obj).GetHashCode();
	}
}
