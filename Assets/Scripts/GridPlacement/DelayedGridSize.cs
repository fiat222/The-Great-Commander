using UnityEngine;

namespace DT.GridSystem
{
	/// <summary>
	/// Attribute used to mark a Vector2Int field as a delayed grid size field.
	/// The property drawer will use DelayedIntField for both X and Y components,
	/// ensuring the value only updates after you finish typing (press Enter or lose focus).
	/// This prevents data loss when resizing the grid mid-typing.
	/// </summary>
	public class DelayedGridSizeAttribute : PropertyAttribute
	{
		public DelayedGridSizeAttribute()
		{
			// Constructor for future extensibility
		}
	}
}
