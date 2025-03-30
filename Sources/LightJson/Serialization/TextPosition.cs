namespace LightJson.Serialization {
	/// <summary>
	/// Represents a position within a plain text resource.
	/// </summary>
	public readonly struct TextPosition {
		/// <summary>
		/// The column position, 0-based.
		/// </summary>
		public readonly long Column;

		/// <summary>
		/// The line position, 0-based.
		/// </summary>
		public readonly long Line;

		/// <summary>
		/// Creates an new instance of the <see cref="TextPosition"/> structure.
		/// </summary>
		/// <param name="line">The 1-based line number.</param>
		/// <param name="column">The 1-based column number.</param>
		public TextPosition ( long line, long column ) {
			this.Column = column;
			this.Line = line;
		}
	}
}
