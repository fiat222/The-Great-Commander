#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DT.GridSystem
{
	[CustomPropertyDrawer(typeof(DelayedGridSizeAttribute))]
	public class GridSizeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);

			Vector2Int currentValue = property.vector2IntValue;

			// Split the rect in half for X and Y
			float halfWidth = position.width / 2f - 2f;
			Rect xRect = new Rect(position.x, position.y, halfWidth, position.height);
			Rect yRect = new Rect(position.x + halfWidth + 4f, position.y, halfWidth, position.height);

			// Use delayed fields
			int newX = EditorGUI.DelayedIntField(xRect, currentValue.x);
			int newY = EditorGUI.DelayedIntField(yRect, currentValue.y);

			property.vector2IntValue = new Vector2Int(newX, newY);

			EditorGUI.EndProperty();
		}
	}
}
#endif