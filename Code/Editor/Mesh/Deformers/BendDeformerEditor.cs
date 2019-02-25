﻿using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Beans.Unity.Editor;
using Deform;

namespace DeformEditor
{
	[CustomEditor (typeof (BendDeformer)), CanEditMultipleObjects]
	public class BendDeformerEditor : DeformerEditor
	{
		private class Content
		{
			public static readonly GUIContent Angle = new GUIContent (text: "Angle", tooltip: "How many degrees the mesh should be bent by the time it reaches the top bounds.");
			public static readonly GUIContent Factor = DeformEditorGUIUtility.DefaultContent.Factor;
			public static readonly GUIContent Mode = new GUIContent (text: "Mode", tooltip: "Unlimited: Entire mesh is bent.\nLimited: Mesh is only bent between bounds.");
			public static readonly GUIContent Top = new GUIContent (text: "Top", tooltip: "Any vertices above this will have been fully bent.");
			public static readonly GUIContent Bottom = new GUIContent (text: "Bottom", tooltip: "Any vertices below this will be fully unbent.");
			public static readonly GUIContent Axis = DeformEditorGUIUtility.DefaultContent.Axis;
		}

		private class Properties
		{
			public SerializedProperty Angle;
			public SerializedProperty Factor;
			public SerializedProperty Mode;
			public SerializedProperty Top;
			public SerializedProperty Bottom;
			public SerializedProperty Axis;

			public Properties (SerializedObject obj)
			{
				Angle	= obj.FindProperty ("angle");
				Factor	= obj.FindProperty ("factor");
				Mode	= obj.FindProperty ("mode");
				Top		= obj.FindProperty ("top");
				Bottom	= obj.FindProperty ("bottom");
				Axis	= obj.FindProperty ("axis");
			}
		}

		private Properties properties;

		private ArcHandle angleHandle = new ArcHandle ();
		private VerticalBoundsHandle boundsHandle = new VerticalBoundsHandle ();

		protected override void OnEnable ()
		{
			base.OnEnable ();

			properties = new Properties (serializedObject);

			boundsHandle.handleCapFunction = DeformHandles.HandleCapFunction;
			boundsHandle.drawGuidelineCallback = (a, b) => DeformHandles.Line (a, b, DeformHandles.LineMode.LightDotted);
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();

			serializedObject.UpdateIfRequiredOrScript ();

			EditorGUILayout.PropertyField (properties.Angle, Content.Angle);
			EditorGUILayout.PropertyField (properties.Factor, Content.Factor);
			EditorGUILayout.PropertyField (properties.Mode, Content.Mode);

			using (new EditorGUI.IndentLevelScope ())
			{
				EditorGUILayoutx.MinField (properties.Top, properties.Bottom.floatValue, Content.Top);
				EditorGUILayoutx.MaxField (properties.Bottom, properties.Top.floatValue, Content.Bottom);
			}

			EditorGUILayout.PropertyField (properties.Axis, Content.Axis);

			serializedObject.ApplyModifiedProperties ();

			EditorApplication.QueuePlayerLoopUpdate ();
		}

		public override void OnSceneGUI ()
		{
			base.OnSceneGUI ();

			var bend = target as BendDeformer;

			boundsHandle.handleColor = DeformEditorSettings.SolidHandleColor;
			boundsHandle.screenspaceHandleSize = DeformEditorSettings.ScreenspaceSliderHandleCapSize;
			if (boundsHandle.DrawHandle (bend.Top, bend.Bottom, bend.Axis, Vector3.up))
			{
				Undo.RecordObject (bend, "Changed Bounds");
				bend.Top = boundsHandle.top;
				bend.Bottom = boundsHandle.bottom;
			}

			DrawAngleHandle (bend);

			EditorApplication.QueuePlayerLoopUpdate ();
		}

		private void DrawAngleHandle (BendDeformer bend)
        {
			angleHandle.angle = bend.Angle;
			angleHandle.radius = HandleUtility.GetHandleSize (bend.Axis.position) * DeformEditorSettings.ScreenspaceAngleHandleSize;
			angleHandle.fillColor = Color.clear;

			var direction = -bend.Axis.right;
			var normal = -bend.Axis.forward;
			var matrix = Matrix4x4.TRS (bend.Axis.position + bend.Axis.rotation * (Vector3.up * bend.Bottom), Quaternion.LookRotation (direction, normal), Vector3.one);

			using (new Handles.DrawingScope (Handles.matrix))
			{
            	Handles.color = DeformEditorSettings.SolidHandleColor;
				Handles.matrix = matrix;

				using (var check = new EditorGUI.ChangeCheckScope ())
				{
					angleHandle.DrawHandle ();
					if (check.changed)
					{
						Undo.RecordObject (bend, "Changed Angle");
						bend.Angle = angleHandle.angle;
					}
				}
			}
        }
	}
}