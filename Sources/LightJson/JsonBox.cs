using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson;

/// <summary>
/// Represents a value box where it's value is serialized and deserialized as an Json value.
/// </summary>
/// <typeparam name="TValue">The value type. This type cannot be null.</typeparam>
public class JsonBox<TValue> where TValue : notnull
{
	private JsonValue jval;

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
	/// Gets or sets the inner <typeparamref name="TValue"/> contained in this <see cref="JsonBox{TValue}"/>.
	/// </summary>
	public TValue Value
	{
		get
		{
			return jval.Get<TValue>();
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			jval = JsonValue.Serialize(value);
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

	/// 
	public static implicit operator TValue(JsonBox<TValue> box)
		=> box.Value;

	/// 
	public static implicit operator JsonBox<TValue>(TValue value)
		=> new JsonBox<TValue>(value);
}
