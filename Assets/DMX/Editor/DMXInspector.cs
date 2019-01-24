using UnityEngine;
using System.Collections;
using UnityEditor;

[ExecuteInEditMode]
[CustomEditor(typeof(DP.DMX))]
public class DMXInspector : Editor {
	
	public override void OnInspectorGUI()
	{
		var script = (DP.DMX) target;

        // TODO: copy missing device handing from MidiEndpointEditor
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Serial ports: ");
        string[] serialPorts = script.serialPorts.ToArray();
        int i = EditorGUILayout.Popup(script.serialPortIdx, serialPorts);
        if (i < serialPorts.Length && i != script.serialPortIdx)
		{
			script.serialPortIdx = i;
			script.OpenSerialPort();
		}
		EditorGUILayout.EndHorizontal();
	}
}