using UnityEngine;
using UnityEditor;

namespace Klak.Wiring
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ColorComponents))]
    public class ColorComponentsEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, new string[] {"m_Script"});

            serializedObject.ApplyModifiedProperties();
        }
    }
}