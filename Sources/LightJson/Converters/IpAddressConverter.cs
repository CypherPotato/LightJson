﻿using System;
using System.Net;

namespace LightJson.Converters;

/// <summary>
/// Represents an <see cref="JsonConverter"/> which can serialize and deserialize <see cref="IPAddress"/> values.
/// </summary>
public sealed class IpAddressConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(IPAddress);
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return IPAddress.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value, JsonOptions currentOptions)
	{
		var str = ((IPAddress)value).ToString();
		return new JsonValue(str);
	}
}
