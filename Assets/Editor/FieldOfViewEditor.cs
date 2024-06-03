using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewEditor : Editor
{
    //variable for to check which button was pressed
    int buttonCheck = -1;
    //GUIStyle variable for to change font size, font color and fond style of characters typed in the "About" section of inspector
    private GUIStyle guiStyle = new GUIStyle();
    //string variable for topic of the "About" section
    string aboutTopic = "Field of View for Unity3D\nVersion: 1.0\n";
   
    //to access variables from "FieldOfView.cs"
    SerializedProperty Radius;
    SerializedProperty Angle;
    SerializedProperty targetMask;
    SerializedProperty obsMask;
    SerializedProperty fovColor;
    SerializedProperty plane;
    SerializedProperty detectedTargets;
    SerializedProperty meshR;
    SerializedProperty visualFilter;

    
    //variable for color of field of view
    //Color fovColor;

    private void OnEnable()
    {
        //to find variables from "FieldOfView.cs"
        Radius = serializedObject.FindProperty("Radius");
        Angle = serializedObject.FindProperty("Angle");
        targetMask = serializedObject.FindProperty("targetMask");
        obsMask = serializedObject.FindProperty("obsMask");
        fovColor = serializedObject.FindProperty("fovColor");
        plane = serializedObject.FindProperty("plane");
        detectedTargets = serializedObject.FindProperty("detectedTargets");
        meshR = serializedObject.FindProperty("meshR");
        visualFilter = serializedObject.FindProperty("visualFilter");
    }


    void OnSceneGUI()
    { 
        //fow object represents field of view target
        FieldOfView fow = (FieldOfView)target;

        //if you want to draw a circle, make color to white which is under the below
        Handles.color = Color.white;

        //for draw a circle around the character
        Handles.DrawWireDisc(fow.transform.position, fow.discVector, fow.Radius);
        //changing the color of circle
        Handles.color = Color.white;
        //half of a angle of field of view
        Vector3 angleA = fow.DirectionFromAngle(false, fow.Angle / 2);
        //other half of a angle of field of view
        Vector3 angleB = fow.DirectionFromAngle(false, -fow.Angle / 2);

        //draw line for visuable point(field of view)
        Handles.DrawLine(fow.transform.position, fow.transform.position + angleA * fow.Radius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + angleB * fow.Radius);

        //draw only field of view
        Handles.DrawWireArc(fow.transform.position, fow.arcVector, angleA, fow.Angle, fow.Radius);
        
        //color of field of view (by variable)
        //note: if you define color of field of view by function use ChangeColor function etc:Handles.color = ChangeColor("color",float alpha value(to set transparency of visual cone))
        Handles.color = fow.fovColor;
        //draw field of view
        Handles.DrawSolidArc(fow.transform.position, fow.arcVector, angleA, fow.Angle, fow.Radius);

        //check is enemy or target in field of view or not
        if (fow.anyObjectDetected)
        {
            //color of line which is show to target
            Handles.color = Color.blue;
            //loop for draw line to target
            foreach(Transform visableTarget in fow.detectedTargets)
            Handles.DrawLine(fow.transform.position, visableTarget.position);
        }
        
    }
    //to make custom inspector of script
    public override void OnInspectorGUI()
    {

        //to set the buttons horizontally
        GUILayout.BeginHorizontal();

        //to check if the "Settings" button has been clicked
        if (GUILayout.Button("Settings"))
        {
            //1 means "settings" button is clicked
            buttonCheck = 1;
        }

        //to check if the "About" button has been clicked
        else if (GUILayout.Button("About"))
        {
            //0 means "About" button is clicked
            buttonCheck = 0;
        }
        //end horizontal part of inspector
        GUILayout.EndHorizontal();

        //if the "Settings" button is clicked 
        if(buttonCheck == 1)
        {
            //for update every variables that we use
            serializedObject.Update();

            //to access variables on inspector
            EditorGUILayout.PropertyField(Radius);
            EditorGUILayout.PropertyField(Angle);
            EditorGUILayout.PropertyField(targetMask);
            EditorGUILayout.PropertyField(obsMask);
            EditorGUILayout.PropertyField(fovColor);
            EditorGUILayout.PropertyField(plane);
            EditorGUILayout.PropertyField(detectedTargets);
            EditorGUILayout.PropertyField(meshR);
            EditorGUILayout.PropertyField(visualFilter);

            //for apply any modifications of variables
            serializedObject.ApplyModifiedProperties();
        }

        //if the "About" button is clicked
        else if(buttonCheck == 0)
        {
            //to define font size of topic
            guiStyle.fontSize = 15;

            //to define font style of topic 
            guiStyle.fontStyle = FontStyle.Bold;

            //to define color of topic
            guiStyle.normal.textColor = Color.white;

            //to write topic in "About" section
            GUILayout.Label(aboutTopic,guiStyle);
            
        }
    }      
     
}

