using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(BezierCurve))]
public class BezierCurveEditor : Editor
{
	private BezierCurve bezierCurve = null;
	private BezierCurveData bezierCurveData = null;
	private bool showSettings = false;
	private int numberOfSampledPointsOnCurve = 0;
	private string bezierCurveDataName = "";
	private List<CurvePoint> curvePoints = null;

	private void Init ()
	{
		if (bezierCurve == null) {
			bezierCurve = (BezierCurve)target;
		}

		RefreshCurvePoints ();
	}

	private void OnEnable ()
	{
		Init ();
	}

	public override void OnInspectorGUI ()
	{
		GUIStyle style = new GUIStyle ();
		style.fontStyle = FontStyle.Bold;

		showSettings = EditorGUILayout.Foldout (showSettings, "Show settings");
		if (showSettings) {
			GUILayout.Label (string.Format ("Points on curve: {0}", numberOfSampledPointsOnCurve));
			GUILayout.Label ("Load/Save", style);
			EditorGUILayout.BeginHorizontal ();
			bezierCurveData = (BezierCurveData)EditorGUILayout.ObjectField ("Bezier curve", bezierCurveData, typeof(BezierCurveData), false);
			if (GUILayout.Button ("Load", GUILayout.Width (40), GUILayout.Height (15))) {
				LoadCurve (bezierCurveData);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			bezierCurveDataName = EditorGUILayout.TextField ("Bezier curve name", bezierCurveDataName);
			if (GUILayout.Button ("Save", GUILayout.Width (40), GUILayout.Height (15))) {
				SaveCurve (bezierCurveDataName);
			}
			EditorGUILayout.EndHorizontal ();

			GUILayout.Label ("Settings", style);
			DrawDefaultInspector ();
		}

		EditorGUILayout.HelpBox ("To start adding points, hold left shift and left click in the 2D scene view.", MessageType.Info);

		GUILayout.Space (10);
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Points", style);
		if (GUILayout.Button ("X", GUILayout.Width (18))) {
			RemoveAllPoints ();
		}
		if (GUILayout.Button ("+", GUILayout.Width (18))) {
			AddPoint ();
		}
		GUILayout.EndHorizontal ();
		GUILayout.Space (5);

		DrawBezierPointControls (bezierCurve);
	}

	private void OnSceneGUI ()
	{
		DrawAddPointsSceneControls ();
		DrawPointsSceneControls ();
		DrawCurve ();
	}

	private void DrawBezierPointControls (BezierCurve bezierCurve)
	{
		List<BezierPoint> points = bezierCurve.GetControlPoints ();
		string[] bezierPointTypes = System.Enum.GetNames (typeof(BezierPointType));

		if (points == null) {
			return;
		}
		
		for (int i = 0; i < points.Count; i++) {
			BezierPoint bezierPoint = points [i];

			GUILayout.BeginHorizontal ();
			GUILayout.Label (bezierPoint.name);
			EditorGUI.BeginChangeCheck ();
			BezierPointType pointType = (BezierPointType)EditorGUILayout.Popup ((int)bezierPoint.pointType, bezierPointTypes);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (bezierCurve, "Change Point Type");

				bezierPoint.pointType = pointType;
				SceneView.RepaintAll ();
			}
			if (GUILayout.Button ("X", GUILayout.Width (15), GUILayout.Height (15))) {
				RemovePoint (bezierPoint);
			}
			GUILayout.EndHorizontal ();

			Vector3 position = EditorGUILayout.Vector3Field ("    Position: ", bezierPoint.position);

			if (bezierPoint.pointType != BezierPointType.None) {
				Vector3 handle1 = EditorGUILayout.Vector3Field ("    Handle 1: ", bezierPoint.handle1);
				Vector3 handle2 = EditorGUILayout.Vector3Field ("    Handle 2: ", bezierPoint.handle2);

				if (bezierPoint.GetHandle1LocalPosition () != handle1) {
					Undo.RecordObject (target, "Move Handle Point 1");
					bezierPoint.handle1 = handle1;
				}
				if (bezierPoint.GetHandle2LocalPosition () != handle2) {
					Undo.RecordObject (target, "Move Handle Point 2");
					bezierPoint.handle2 = handle2;
				}
			}

			if (bezierPoint.GetLocalPosition () != position) {
				Undo.RecordObject (target, "Move Point");
				bezierPoint.SetPosition (position);
			}
		}


	}

	private void DrawAddPointsSceneControls ()
	{
		if (curvePoints == null || !Event.current.shift) {
			return;
		}

		Vector3 mousePosition = GetMouseWorldPosition ();
		Vector2 mousePosition2D = new Vector2 (mousePosition.x, mousePosition.y);
		CurvePoint addCurvePoint = null;

		for (int i = 0; i < curvePoints.Count; i++) {
			Vector2 curvePoint2D = new Vector2 (curvePoints [i].position.x, curvePoints [i].position.y);
			float distance = (mousePosition2D - curvePoint2D).magnitude;
			if (distance < 0.4f) {
				Handles.CircleCap (0, curvePoints [i].position, Quaternion.identity, HandleUtility.GetHandleSize (curvePoints [i].position) * bezierCurve.handleSize);
				Handles.Label (curvePoints [i].position + Vector3.right, curvePoints [i].curveIndex.ToString ());
				SceneView.RepaintAll ();
				addCurvePoint = curvePoints [i];
				break;
			}
		}

		if (Event.current.type == EventType.mouseDown && Event.current.button == 0) {
			if (Event.current.shift) {
				if (addCurvePoint != null) {
					AddPoint (addCurvePoint.position, new Vector3 (-1, 0, 0), new Vector3 (1, 0, 0), addCurvePoint.curveIndex);
				} else {
					AddPoint (new Vector3 (mousePosition2D.x, mousePosition2D.y, 0), new Vector3 (-1, 0, 0), new Vector3 (1, 0, 0), 0);
				}
			}
		}
	}

	private void DrawPointsSceneControls ()
	{
		if (bezierCurve == null || bezierCurve.GetControlPoints ().Count == 0) {
			return;
		}

		List<BezierPoint> points = bezierCurve.GetControlPoints ();

		for (int i = 0; i < points.Count; i++) {
			BezierPoint bezierPoint = points [i];
			float handleSize = HandleUtility.GetHandleSize (bezierPoint.position) * bezierCurve.handleSize;
			Vector3 position = Handles.FreeMoveHandle (bezierPoint.GetPosition (), Quaternion.identity, handleSize, GetSnapSize (), Handles.RectangleCap);

			Handles.Label (bezierPoint.GetPosition () + Vector3.right * 0.5f, bezierPoint.name);

			if (bezierPoint.GetLocalPosition () != position) {
				Undo.RecordObject (target, "Move Point");
				bezierPoint.SetPosition (position);
			}

			if (bezierPoint.pointType != BezierPointType.None) {
				Vector3 handle1 = Handles.FreeMoveHandle (bezierPoint.GetHandle1Position (), Quaternion.identity, handleSize, GetSnapSize (), Handles.CircleCap);
				Vector3 handle2 = Handles.FreeMoveHandle (bezierPoint.GetHandle2Position (), Quaternion.identity, handleSize, GetSnapSize (), Handles.CircleCap);

				if (bezierPoint.pointType == BezierPointType.Connected) {
					Quaternion rotation = Handles.Disc (bezierPoint.GetHandlesRotation (), bezierPoint.GetPosition (), Vector3.forward, handleSize * 4.0f, true, 15);

					if (bezierPoint.GetHandlesRotation () != rotation) {
						Undo.RecordObject (target, "Rotate handle");
						bezierPoint.SetHandlesRotation (rotation);

						continue;
					}
				}

				Handles.DrawLine (position, handle1);
				Handles.DrawLine (position, handle2);

				int handleToAdjust = 0;

				if (bezierPoint.GetHandle1Position () != handle1) {
					Undo.RecordObject (target, "Move Handle Point 1");
					bezierPoint.SetHandle1Position (handle1);

					if (bezierPoint.pointType == BezierPointType.Connected) {
						handleToAdjust = 2;
					}
				}
				if (bezierPoint.GetHandle2Position () != handle2) {
					Undo.RecordObject (target, "Move Handle Point 2");
					bezierPoint.SetHandle2Position (handle2);

					if (bezierPoint.pointType == BezierPointType.Connected) {
						handleToAdjust = 1;
					}
				}

				if (bezierPoint.pointType == BezierPointType.Connected) {
					if (handleToAdjust == 1) {
						bezierPoint.SetHandle1Position (position + (handle2 - position) * -1.0f);
					}
					if (handleToAdjust == 2) {
						bezierPoint.SetHandle2Position (position + (handle1 - position) * -1.0f);
					}
				}
			}
		}
	}

	private void RefreshCurvePoints ()
	{
		curvePoints = bezierCurve.GetPoints (bezierCurve.sampleRate, includeLastPoint: true);
	}

	private void DrawCurve ()
	{
		RefreshCurvePoints ();

		List<Vector3> polyLinePoints = new List<Vector3> ();
		numberOfSampledPointsOnCurve = curvePoints.Count;

		for (int i = 0; i < curvePoints.Count; i++) {
			CurvePoint curvePoint = curvePoints [i];

			Handles.color = Color.red;
			Handles.DrawSolidDisc (curvePoint.position, Vector3.forward, 0.05f);
			Handles.color = Color.white;

			polyLinePoints.Add (curvePoint.position);
		}

		Handles.DrawPolyLine (polyLinePoints.ToArray ());
	}

	private Vector3 GetSnapSize ()
	{
		return Vector3.one * bezierCurve.snapSize;
	}

	private Vector3 GetMouseWorldPosition ()
	{
		Vector3 mousePosition = Event.current.mousePosition;
		mousePosition.y = Camera.current.pixelHeight - mousePosition.y;
		return Camera.current.ScreenPointToRay (mousePosition).origin;
	}

	private void AddPoint ()
	{
		Undo.RecordObject (bezierCurve, "Add Point");

		Vector3 position = bezierCurve.transform.position + Vector3.up * 5.0f;
		Vector3 handle1 = new Vector3 (-1, 0, 0);
		Vector3 handle2 = new Vector3 (1, 0, 0);

		AddPoint (position, handle1, handle2, 0);
	}

	private void AddPoint (Vector3 position, Vector3 handle1, Vector3 handle2, int curveIndex = 0)
	{
		Undo.RecordObject (bezierCurve, "Add Point");

		BezierPoint bezierPoint = new BezierPoint (position, handle1, handle2);
		bezierCurve.AddControlPoint (bezierPoint, curveIndex);
		bezierPoint.SetPosition (position); // wierd code :(

		SceneView.RepaintAll ();
	}

	private void RemoveAllPoints ()
	{
		Undo.RecordObject (bezierCurve, "Remove All Points");

		bezierCurve.RemoveAllControlPoints ();
		SceneView.RepaintAll ();
	}

	private void RemovePoint (BezierPoint bezierPoint)
	{
		Undo.RecordObject (bezierCurve, "Remove Point");

		bezierCurve.RemoveControlPoint (bezierPoint);
		SceneView.RepaintAll ();
	}

	private void LoadCurve (BezierCurveData bezierCurveData)
	{
		RemoveAllPoints ();

		bezierCurve.Load (bezierCurveData);
	}

	private void SaveCurve (string curveName)
	{
		string assetPath = "Assets/Curves/" + curveName + ".asset";
		bezierCurve.Save (assetPath);
	}
}
