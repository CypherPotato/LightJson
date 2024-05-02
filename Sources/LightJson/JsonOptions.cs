using LightJson.Converters;
using LightJson.Serialization;
using System.Text.Json;

namespace LightJson;

#nullable enable

/// <summary>
/// Provides options and configurations for using the LightJson library.
/// </summary>
public class JsonOptions
{
	private readonly static JsonOptions _default = new JsonOptions();

	/// <summary>
	/// Gets or sets the default <see cref="JsonOptions"/> object.
	/// </summary>
	public static JsonOptions Default { get; set; } = _default;

	/// <summary>
	/// Gets or sets serialization flags to the <see cref="Serialization.JsonReader"/>.
	/// </summary>
	public JsonSerializationFlags SerializationFlags { get; set; } = default;

	/// <summary>
	/// Gets or sets whether the JSON serializer should write indentend, pretty formatted
	/// outputs.
	/// </summary>
	public bool WriteIndented { get; set; } = false;

	/// <summary>
	/// Gets or sets a value that indicates whether a property's name uses a case-insensitive comparison when getting
	/// values.
	/// </summary>
	public bool PropertyNameCaseInsensitive { get; set; } = false;

	/// <summary>
	/// Gets or sets whether the <see cref="JsonValue.Serialize(object?, JsonOptions?)"/> should serialize fields from
	/// types.
	/// </summary>
	public bool SerializeFields { get; set; } = false;

	/// <summary>
	/// Gets or sets an list of <see cref="JsonConverter"/>.
	/// </summary>
	public JsonConverterCollection Converters { get; set; }

	/// <summary>
	/// Gets or sets the function that transforms the property name of a JSON object to JSON output.
	/// </summary>
	public JsonNamingPolicy? NamingPolicy { get; set; }

	/// <summary>
	/// Gets or sets an boolean indicating where the JSON parser should throw on duplicated object keys.
	/// </summary>
	public bool ThrowOnDuplicateObjectKeys { get; set; } = false;

	/// <summary>
	/// Creates an new <see cref="JsonOptions"/> instance with default parameters.
	/// </summary>
	public JsonOptions()
	{
		Converters = new JsonConverterCollection(this)
		{
			new DictionaryConverter(),
			new GuidConverter(),
			new EnumConverter(),
			new DateTimeConverter(),
			new DateOnlyConverter(),
			new TimeOnlyConverter(),
			new TimeSpanConverter(),
			new CharConverter(),
			new DecimalConverter()
		};
	}

	/// <summary>
	/// Creates an new empty <see cref="JsonObject"/> instance using this <see cref="JsonOptions"/> 
	/// options.
	/// </summary>
	public JsonObject CreateJsonObject() => new JsonObject(this);

	/// <summary>
	/// Creates an new empty <see cref="JsonArray"/> instance using this <see cref="JsonOptions"/> 
	/// options.
	/// </summary>
	public JsonArray CreateJsonArray() => new JsonArray(this);
}
