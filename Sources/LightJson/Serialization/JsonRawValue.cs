using System;
using System.IO;
using System.Threading.Tasks;

namespace LightJson.Serialization;

/// <summary>
/// Represents a raw JSON value that can be serialized directly to a <see cref="System.IO.TextWriter"/>.
/// </summary>
public abstract class JsonRawValue : IImplicitJsonValue {

	/// <summary>
	/// Calculates the length of the JSON value when serialized with the specified <see cref="JsonOptions"/>.
	/// </summary>
	/// <param name="options">The options controlling JSON serialization.</param>
	/// <returns>The length in characters of the serialized JSON value.</returns>
	public virtual long CalculateLength ( JsonOptions options ) {
		return 0;
	}

	/// <summary>
	/// Serializes the JSON value to the specified <see cref="System.IO.TextWriter"/> synchronously.
	/// </summary>
	/// <param name="writer">The writer to which the JSON value will be written.</param>
	public abstract void SerializeToStream ( TextWriter writer );

	/// <summary>
	/// Serializes the JSON value to the specified <see cref="System.IO.TextWriter"/> asynchronously.
	/// </summary>
	/// <param name="writer">The writer to which the JSON value will be written.</param>
	/// <returns>A <see cref="System.Threading.Tasks.Task"/> that represents the asynchronous operation.</returns>
	public virtual Task SerializeToStreamAsync ( TextWriter writer ) {
		SerializeToStream ( writer );
		return Task.CompletedTask;
	}

	/// <summary>
	/// Creates a <see cref="JsonRawValue"/> that writes its content using the provided <see cref="Action{T}"/> delegate.
	/// </summary>
	/// <param name="writeAction">An action that writes the JSON content to a <see cref="System.IO.TextWriter"/>.</param>
	/// <returns>A <see cref="JsonRawValue"/> instance that uses the supplied action for serialization.</returns>
	public static JsonRawValue FromAction ( Action<TextWriter> writeAction ) {
		return new InlineJsonRawValue ( writeAction );
	}

	/// <summary>
	/// Creates a <see cref="JsonRawValue"/> that writes its content using the provided asynchronous <see cref="Func{TResult}"/> delegate.
	/// </summary>
	/// <param name="writeActionAsync">A function that returns a <see cref="System.Threading.Tasks.Task"/> which writes the JSON content to a <see cref="System.IO.TextWriter"/>.</param>
	/// <returns>A <see cref="JsonRawValue"/> instance that uses the supplied asynchronous function for serialization.</returns>
	public static JsonRawValue FromAction ( Func<Task<TextWriter>> writeActionAsync ) {
		return new InlineJsonRawValue ( writeActionAsync );
	}

	/// <inheritdoc/>
	public string ToString ( JsonOptions options ) {
		return options.SerializeJson ( this );
	}

	/// <inheritdoc/>
	public JsonValue AsJsonValue () {
		return new JsonValue ( JsonValueType.Raw, default, this, JsonOptions.Default );
	}
}

class InlineJsonRawValue : JsonRawValue {

	Action<TextWriter> _writeAction;
	Func<Task<TextWriter>>? _writeActionAsync;

	public InlineJsonRawValue ( Action<TextWriter> writeAction ) {
		_writeAction = writeAction;
	}

	public InlineJsonRawValue ( Func<Task<TextWriter>> writeActionAsync ) {
		_writeActionAsync = writeActionAsync;
		_writeAction = writer => _writeActionAsync!.Invoke ().GetAwaiter ().GetResult ();
	}

	public override void SerializeToStream ( TextWriter writer ) {
		_writeAction ( writer );
	}

	public override Task SerializeToStreamAsync ( TextWriter writer ) {
		if (_writeActionAsync != null) {
			return _writeActionAsync ().ContinueWith ( t => {
				if (t.IsFaulted) {
					throw t.Exception!;
				}
				_writeAction ( writer );
			} );
		}
		else {
			SerializeToStream ( writer );
			return Task.CompletedTask;
		}
	}
}