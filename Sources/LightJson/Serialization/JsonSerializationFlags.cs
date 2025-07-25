using System;

namespace LightJson.Serialization;

/// <summary>
/// Specifies special configurations and flags for JSON serializers.
/// </summary>
[Flags]
public enum JsonSerializationFlags
{
	/// <summary>
	/// Defines that the <see cref="Serialization.JsonReader"/> should ignore inline and multi-line JavaScript-like comments.
	/// </summary>
	IgnoreComments = 1 << 0,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> can read unquoted JSON properties names.
	/// </summary>
	AllowUnquotedPropertyNames = 1 << 2,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should ignore trailing commas.
	/// </summary>
	IgnoreTrailingComma = 1 << 3,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow string literals line breaks.
	/// </summary>
	AllowStringLineBreaks = 1 << 4,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow leading decimal points.
	/// </summary>
	LeadingDecimalPoint = 1 << 6,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow trailing decimal points.
	/// </summary>
	TrailingDecimalPoint = 1 << 7,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow hexadecimal (0x...) number literals.
	/// </summary>
	HexadecimalNumberLiterals = 1 << 8,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow the positive sign (+) in number literals.
	/// </summary>
	AllowPositiveSign = 1 << 9,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow string literals delimited by single quotes.
	/// </summary>
	AllowSingleQuotes = 1 << 10,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should ignore numeric underscore literals.
	/// </summary>
	NumericUnderscoreLiterals = 1 << 11,

	/// <summary>
	/// Defines that the <see cref="JsonReader"/> should allow multiline string literals.
	/// </summary>
	MultilineStringLiterals = 1 << 12,

	/// <summary>
	/// Defines that all JSON5 are defined.
	/// </summary>
	Json5 =
		IgnoreComments |
		AllowUnquotedPropertyNames |
		IgnoreTrailingComma |
		AllowStringLineBreaks |
		TrailingDecimalPoint |
		LeadingDecimalPoint |
		HexadecimalNumberLiterals |
		AllowPositiveSign |
		AllowSingleQuotes |
		NumericUnderscoreLiterals,

	/// <summary>
	/// Defines that all flags are defined
	/// </summary>
	All =
		Json5 |
		MultilineStringLiterals
}
