namespace LightJson;

/// <summary>
/// Represents the base generic interface for the <see cref="JsonBox{TValue}"/> class.
/// </summary>
public interface IJsonBox
{
	/// <summary>
	/// Represents the contained JSON value.
	/// </summary>
	public JsonValue JsonValue { get; set; }
}
