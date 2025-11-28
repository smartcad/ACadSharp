using System;

namespace ACadSharp
{
	/// <summary>
	/// Event args for collection changed events.
	/// </summary>
	/// <remarks>
	/// Note: This class is designed to be reused to minimize allocations.
	/// Do not store references to instances of this class as they may be reused.
	/// </remarks>
	public class CollectionChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Item that is being added or removed from the collection
		/// </summary>
		public CadObject Item { get; private set; }

		public CollectionChangedEventArgs(CadObject item)
		{
			this.Item = item;
		}

		/// <summary>
		/// Creates or reuses a CollectionChangedEventArgs instance.
		/// </summary>
		/// <param name="item">The item being added or removed.</param>
		/// <returns>A CollectionChangedEventArgs instance.</returns>
		/// <remarks>
		/// This method may return a reused instance to reduce allocations.
		/// Do not store the returned instance.
		/// </remarks>
		internal static CollectionChangedEventArgs Create(CadObject item)
		{
			// For now, create new instances until thread-safety can be evaluated.
			// Future optimization: use thread-local pooling for high-frequency scenarios.
			return new CollectionChangedEventArgs(item);
		}

		/// <summary>
		/// Resets this instance for reuse.
		/// </summary>
		internal void Reset(CadObject item)
		{
			this.Item = item;
		}
	}
}
