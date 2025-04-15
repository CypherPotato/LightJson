using System.Text.Json;

namespace LightJson;

/// <summary>
/// Specifies what default options are used by <see cref="JsonOptions"/>.
/// </summary>
public enum JsonOptionsDefaults
{
	/// <summary>
	/// Specifies that general-purpose values should be used. These are the same settings applied if a <see cref="JsonSerializerDefaults"/> isn't specified.
	/// </summary>
	/// <remarks>
	/// This option implies that property names are treated as case-sensitive and no name formatting is applied.
	/// </remarks>
	General = 0,

	/// <summary>
	/// Specifies that values should be used more appropriate to web-based scenarios.
	/// </summary>
	/// <remarks>
	/// <para>This option implies that:</para>
	/// <para>- Property names are sanitized (case-insensitive and only alphanumeric chars are used in comparisons),</para>
	/// <para>- "camelCase" name formatting should be employed.</para>
	/// <para>- Numbers can be deserialized as strings.</para>
	/// </remarks>
	Web = 1
}
