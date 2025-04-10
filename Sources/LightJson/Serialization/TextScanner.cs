using System;
using System.IO;
using System.Threading;

namespace LightJson.Serialization {
	using ErrorType = JsonParseException.ErrorType;

	/// <summary>
	/// Represents a text scanner that reads one character at a time.
	/// </summary>
	public sealed class TextScanner : IDisposable {
		private readonly TextReader reader;
		internal bool CanThrowExceptions;
		internal Exception? Exception;
		private bool disposedValue;

		int currentLine;
		int currentColumn;

		T ThrowParseException<T> ( Exception ex ) {
			if (this.CanThrowExceptions) {
				throw ex;
			}
			else {
				this.Exception = ex;
				return default ( T )!;
			}
		}

		/// <summary>
		/// Gets the position of the scanner within the text.
		/// </summary>
		public TextPosition Position {
			get => new TextPosition ( this.currentLine, this.currentColumn );
		}

		/// <summary>
		/// Gets a value indicating whether there are still characters to be read.
		/// </summary>
		public bool CanRead {
			get {
				return this.reader.Peek () != -1;
			}
		}

		/// <summary>
		/// Initializes a new instance of TextScanner.
		/// </summary>
		/// <param name="reader">The TextReader to read the text.</param>
		public TextScanner ( TextReader reader ) {
			this.reader = reader ?? throw new ArgumentNullException ( nameof ( reader ) );
		}

		/// <summary>
		/// Returns whether the specified char is an value terminator.
		/// </summary>
		/// <param name="n">The char to check.</param>
		public static bool IsNumericValueTerminator ( char n )
			=> n == ','
			|| n == '}'
			|| n == ']'
			|| n == '\0'
			|| n == ' '
			|| n == '\n'
			|| n == '\r'
			|| n == '\t';

		/// <summary>
		/// Reads the next character in the stream without changing the current position or returns an null character '\0' if the scanner
		/// reaches the string end.
		/// </summary>
		public char PeekOrDefault () {
			var next = this.reader.Peek ();

			if (next == -1) {
				return '\0';
			}

			return (char) next;
		}

		/// <summary>
		/// Reads the next character in the stream without changing the current position.
		/// </summary>
		public char Peek () {
			var next = this.reader.Peek ();

			if (next == -1) {
				return this.ThrowParseException<char> ( new JsonParseException (
					ErrorType.IncompleteMessage,
					this.Position
				) );
			}

			return (char) next;
		}

		/// <summary>
		/// Reads the next character in the stream, advancing the text position.
		/// </summary>
		public char Read () {
			var next = this.reader.Read ();

			if (next == -1) {
				return this.ThrowParseException<char> ( new JsonParseException (
					ErrorType.IncompleteMessage,
					this.Position
				) );
			}

			switch (next) {
				case '\r':
					// Normalize '\r\n' line encoding to '\n'.
					if (this.reader.Peek () == '\n') {
						this.reader.Read ();
					}
					goto case '\n';

				case '\n':
					this.currentLine += 1;
					this.currentColumn = 0;
					return '\n';

				default:
					this.currentColumn += 1;
					return (char) next;
			}
		}

		/// <summary>
		/// Advances the scanner to next non-whitespace character.
		/// </summary>
		public void SkipWhitespace ( CancellationToken cancellation ) {
			while (char.IsWhiteSpace ( this.PeekOrDefault () )) {
				cancellation.ThrowIfCancellationRequested ();
				this.Read ();
			}
		}

		/// <summary>
		/// Verifies that the given character matches the next character in the stream.
		/// If the characters do not match, an exception will be thrown.
		/// </summary>
		/// <param name="next">An array of expected characters.</param>
		public char AssertAny ( ReadOnlySpan<char> next ) {
			var p = this.Peek ();
			for (int i = 0; i < next.Length; i++) {
				if (next [ i ] == p)
					return p;
			}

			return this.ThrowParseException<char> ( new JsonParseException (
				"Parser expected " + string.Join ( " or ", next.ToArray () ),
				ErrorType.InvalidOrUnexpectedCharacter,
				this.Position
			) );
		}

		/// <summary>
		/// Verifies that the given character matches the next character in the stream.
		/// If the characters do not match, an exception will be thrown.
		/// </summary>
		/// <param name="next">The expected character.</param>
		public void Assert ( char next ) {
			if (this.Peek () == next) {
				this.Read ();
			}
			else {
				this.ThrowParseException<object> ( new JsonParseException (
					string.Format ( "Parser expected '{0}'", next ),
					ErrorType.InvalidOrUnexpectedCharacter,
					this.Position
				) );
			}
		}

		/// <summary>
		/// Verifies that the given string matches the next characters in the stream.
		/// If the strings do not match, an exception will be thrown.
		/// </summary>
		/// <param name="next">The expected string.</param>
		public void Assert ( string next ) {
			try {
				for (var i = 0; i < next.Length; i += 1) {
					this.Assert ( next [ i ] );
				}
			}
			catch (JsonParseException e) when (e.Type == ErrorType.InvalidOrUnexpectedCharacter) {
				this.ThrowParseException<object> ( new JsonParseException (
					string.Format ( "Parser expected '{0}'", next ),
					ErrorType.InvalidOrUnexpectedCharacter,
					this.Position
				) );
			}
		}

		private void Dispose ( bool disposing ) {
			if (!this.disposedValue) {
				if (disposing) {
					this.reader?.Dispose ();
				}

				this.disposedValue = true;
			}
		}

		/// <inheritdoc/>
		public void Dispose () {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose ( disposing: true );
			GC.SuppressFinalize ( this );
		}
	}
}
