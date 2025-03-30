using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="TimeOnly"/> values.
/// </summary>
public sealed class TimeOnlyConverter : JsonConverter {
	/// <inheritdoc/>
	public override bool CanSerialize ( Type type, JsonOptions currentOptions ) {
		return type == typeof ( TimeOnly );
	}

	/// <inheritdoc/>
	public override object Deserialize ( JsonValue value, Type requestedType, JsonOptions currentOptions ) {
		return TimeOnly.Parse ( value.GetString () );
	}

	/// <inheritdoc/>
	public override JsonValue Serialize ( object value, JsonOptions currentOptions ) {
		TimeOnly d = (TimeOnly) value;
		return new JsonValue ( d.ToLongTimeString () );
	}
}
