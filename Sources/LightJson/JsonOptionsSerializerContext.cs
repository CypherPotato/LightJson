using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LightJson;

/// <summary>
/// Provides a context for serializing and deserializing JSON data with options.
/// </summary>
public class JsonOptionsSerializerContext
{
	/// <summary>
	/// Gets the options for the <see cref="System.Text.Json.JsonSerializer"/>.
	/// </summary>
	public JsonSerializerOptions SerializerOptions { get; }

	/// <summary>
	/// Gets the resolver for JSON type information.
	/// </summary>
	public IJsonTypeInfoResolver TypeResolver { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonOptionsSerializerContext"/> class with default options.
	/// </summary>
	/// <remarks>
	/// This constructor uses the default <see cref="JsonSerializerOptions"/> and a <see cref="DefaultJsonTypeInfoResolver"/>.
	/// </remarks>
	[RequiresDynamicCode("This method calls the JsonSerializerOptions.Default, which requires dynamic code.")]
	[RequiresUnreferencedCode("This method calls the JsonSerializerOptions.Default, which requires unreferenced code.")]
	public JsonOptionsSerializerContext()
	{
		this.SerializerOptions = JsonSerializerOptions.Default;
		this.TypeResolver = new DefaultJsonTypeInfoResolver();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonOptionsSerializerContext"/> class with the specified serializer options.
	/// </summary>
	/// <param name="serializerOptions">The options for the <see cref="System.Text.Json.JsonSerializer"/>.</param>
	/// <remarks>
	/// This constructor uses the specified <see cref="JsonSerializerOptions"/> and a <see cref="DefaultJsonTypeInfoResolver"/>.
	/// </remarks>
	[RequiresDynamicCode("This method calls the DefaultJsonTypeInfoResolver.Default, which requires dynamic code.")]
	[RequiresUnreferencedCode("This method calls the DefaultJsonTypeInfoResolver.Default, which requires unreferenced code.")]
	public JsonOptionsSerializerContext(JsonSerializerOptions serializerOptions)
	{
		this.SerializerOptions = serializerOptions;
		this.TypeResolver = new DefaultJsonTypeInfoResolver();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonOptionsSerializerContext"/> class with the specified type resolver and serializer options.
	/// </summary>
	/// <param name="typeResolver">The resolver for JSON type information.</param>
	/// <param name="serializerOptions">The options for the <see cref="System.Text.Json.JsonSerializer"/>.</param>
	public JsonOptionsSerializerContext(IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions)
	{
		this.SerializerOptions = serializerOptions;
		this.TypeResolver = typeResolver;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonOptionsSerializerContext"/> class with the specified type resolver.
	/// </summary>
	/// <param name="typeResolver">The resolver for JSON type information, used to create a new instance of <see cref="JsonSerializerOptions"/>.</param>
	/// <remarks>
	/// This constructor creates a new instance of <see cref="JsonSerializerOptions"/> with the specified <paramref name="typeResolver"/> as its <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
	/// </remarks>
	public JsonOptionsSerializerContext(IJsonTypeInfoResolver typeResolver)
	{
		this.SerializerOptions = new JsonSerializerOptions
		{
			TypeInfoResolver = typeResolver
		};
		this.TypeResolver = typeResolver;
	}

	/// 
	public static implicit operator JsonOptionsSerializerContext(JsonSerializerContext serializerContext) => new JsonOptionsSerializerContext(serializerContext);

	///
	[RequiresDynamicCode("This method calls the DefaultJsonTypeInfoResolver.Default, which requires dynamic code.")]
	[RequiresUnreferencedCode("This method calls the DefaultJsonTypeInfoResolver.Default, which requires unreferenced code.")]
	public static implicit operator JsonOptionsSerializerContext(JsonSerializerOptions serializerOptions) => new JsonOptionsSerializerContext(serializerOptions);
}
