using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="TimeSpan"/> values.
/// </summary>
public sealed class TimeSpanConverter : JsonConverter {
	/// <inheritdoc />
	public override bool CanSerialize ( Type type, JsonOptions currentOptions ) {
		return type == typeof ( TimeSpan );
	}

	/// <inheritdoc />
	public override object Deserialize ( JsonValue value, Type requestedType, JsonOptions currentOptions ) {
		return new TimeSpan ( value.GetLong () );
	}

	/// <inheritdoc />
	public override JsonValue Serialize ( object value, JsonOptions currentOptions ) {
		TimeSpan s = (TimeSpan) value;
		return new JsonValue ( s.Ticks );
	}
}
