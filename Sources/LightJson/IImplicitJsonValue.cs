namespace LightJson;

/// <summary>
/// Represents an value that can be implicitly converted to an <see cref="JsonValue"/>.
/// </summary>
public interface IImplicitJsonValue {
	/// <summary>
	/// Returns a JSON string representing the state of this value.
	/// </summary>
	/// <param name="options">Specifies the <see cref="JsonOptions"/> used to render this Json value.</param>
	public string ToString ( JsonOptions options );

	/// <summary>
	/// Returns an <see cref="JsonValue"/> representating this object.
	/// </summary>
	public JsonValue AsJsonValue ();
}
