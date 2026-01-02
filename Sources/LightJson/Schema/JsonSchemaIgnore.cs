using System;

namespace LightJson.Schema;

/// <summary>
/// Indicates that the decorated member should be ignored when generating a JSON schema.
/// </summary>
/// <remarks>Apply this attribute to classes, properties, fields, or other members to exclude them from dynamic JSON
/// schema generation. This is useful when certain members should not be represented in the schema, such as internal
/// data or implementation details. The attribute can be inherited by derived types, but only one instance can be
/// applied per member.</remarks>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class JsonSchemaIgnore : Attribute
{
}
