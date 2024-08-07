﻿using System;

namespace LightJson;

/// <summary>
/// Represents a value box where it's value is serialized and deserialized as an Json value.
/// </summary>
/// <typeparam name="TValue">The value type. This type cannot be null.</typeparam>
public class JsonBox<TValue> : IEquatable<TValue> where TValue : notnull
{
	private JsonValue jval;

	/// <summary>
	/// Creates an new instance of <see cref="JsonBox{TValue}"/> with no initial
	/// value.
	/// </summary>
	public JsonBox()
	{
		jval = JsonValue.Null;
	}

	/// <summary>
	/// Creates an new instance of <see cref="JsonBox{TValue}"/> with the specified
	/// value.
	/// </summary>
	/// <param name="value">The initial value.</param>
	public JsonBox(TValue value)
	{
		Value = value;
	}

	/// <summary>
	/// Creates an new instance of <see cref="JsonBox{TValue}"/> with the specified
	/// JSON input.
	/// </summary>
	/// <param name="jvalue">The JSON value as the input.</param>
	public JsonBox(JsonValue jvalue)
	{
		this.jval = jvalue;
	}

	/// <summary>
	/// Gets an boolean indicating if this JSON box value is null.
	/// </summary>
	public bool IsNull { get => jval.IsNull; }

	/// <summary>
	/// Gets or sets the inner <typeparamref name="TValue"/> contained in this <see cref="JsonBox{TValue}"/>.
	/// </summary>
	public TValue? Value
	{
		get
		{
			if (IsNull)
			{
				return default;
			}
			else
			{
				return jval.Get<TValue>();
			}
		}
		set
		{
			if (value is null)
			{
				jval = JsonValue.Null;
			}
			else
			{
				jval = JsonValue.Serialize(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets the inner <see cref="JsonValue"/> which represents this <see cref="JsonBox{TValue}"/> contained value.
	/// </summary>
	public JsonValue JsonValue
	{
		get => jval;
		set => jval = value;
	}

	/// <inheritdoc/>
	public override string? ToString()
	{
		return Value?.ToString();
	}

	/// <summary>
	/// Gets an JSON string representation of the current value.
	/// </summary>
	public string ToJsonString() => jval.ToString();

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is JsonBox<TValue> jbox)
		{
			return Value?.Equals(jbox.Value) == true;
		}
		else if (obj is TValue tv)
		{
			return Value?.Equals(tv) == true;
		}
		return false;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return Value?.GetHashCode() ?? 0;
	}

	/// <inheritdoc/>
	public bool Equals(TValue? other)
	{
		return Value?.Equals(other) == true;
	}

	/// 
	public static implicit operator TValue?(JsonBox<TValue> box)
		=> box.Value;

	/// 
	public static implicit operator JsonBox<TValue>(TValue value)
		=> new JsonBox<TValue>(value);
}
