using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Off_Loader))]
public class ExportButtonEditor : Editor
{
    // ^ This is the script we are making a custom editor for.

    public override void OnInspectorGUI()
    {
        //Called whenever the inspector is drawn for this object.
        DrawDefaultInspector();
        //This draws the default screen.  You don't need this if you want
        //to start from scratch, but I use this when I'm just adding a button or
        //some small addition and don't feel like recreating the whole inspector.

        if (GUILayout.Button("Export")) {
            Debug.Log("Starting OFF export...");
            ((Off_Loader)target).export_off();
            Debug.Log("Done !");
        }
    }
    
}