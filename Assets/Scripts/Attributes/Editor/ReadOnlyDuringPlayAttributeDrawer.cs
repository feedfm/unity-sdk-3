using FeedFM.Attributes;
using UnityEngine;
using UnityEditor;
 
[CustomPropertyDrawer(typeof(ReadOnlyDuringPlayAttribute))]
public class ReadOnlyDuringPlayAttributeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
 
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = !Application.isPlaying;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}