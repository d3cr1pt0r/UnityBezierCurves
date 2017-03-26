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
	private bool showCurve = true;
	private bool showSamplePoints = true;
	private bool showNormals = true;
	private bool showTangents = true;
	private string bezierCurveDataName = "";

	private int numberOfSampledPointsOnCurve = 0;
	private List<CurvePoint> curvePoints = null;
	private BezierPoint lastSelectedPoint = null;
	private List<BezierPoint> selectedPoints = null;
	private BezierPoint grabbedBezierPoint = null;
	private BezierPoint mouseOverBezierPoint = null;
	private BezierPoint nearBezierPoint = null;
	private CurvePoint nearCurvePoint = null;

	private void Init ()
	{
		if (bezierCurve == null) {
			bezierCurve = (BezierCurve)target;
		}

		selectedPoints = new List<BezierPoint> ();
		RefreshCurvePoints ();
	}

	private void OnEnable ()
	{
		Init ();
	}

	public override void OnInspectorGUI ()
	{
		EditorGUILayout.HelpBox ("To start adding points, hold left shift and left click in the 2D scene view.", MessageType.Info);
		EditorGUILayout.HelpBox ("To quickly change node type, select a node and: \n  Shift+1  -> Connected\n  Shift+2  -> Broken\n  Shift+3  -> None", MessageType.Info);

		GUIStyle style = new GUIStyle ();
		style.fontStyle = FontStyle.Bold;

		DrawInspectorSettings ();

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

		DrawInspectorBezierPointControls ();
	}

	private void OnSceneGUI ()
	{
		DrawCurve ();

		DrawScenePointControls ();
		NodeTypeShortcuts ();
	}

	private void DrawInspectorSettings ()
	{
		GUIStyle style = new GUIStyle ();
		style.fontStyle = FontStyle.Bold;

		showSettings = EditorGUILayout.Foldout (showSettings, "Show settings");
		if (showSettings) {
			GUILayout.Label ("Debug", style);
			GUILayout.Label (string.Format ("Points on curve: {0}", numberOfSampledPointsOnCurve));
			GUILayout.BeginHorizontal ();
			showCurve = GUILayout.Toggle (showCurve, "Curve");
			showSamplePoints = GUILayout.Toggle (showSamplePoints, "Sample points");
			showNormals = GUILayout.Toggle (showNormals, "Normals");
			showTangents = GUILayout.Toggle (showTangents, "Tangents");
			GUILayout.EndHorizontal ();
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

	}

	private void DrawInspectorBezierPointControls ()
	{
		List<BezierPoint> points = bezierCurve.GetAnchorPoints ();
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

	private void DrawScenePointControls ()
	{
		if (bezierCurve == null) {
			return;
		}

		List<BezierPoint> bezierPoints = bezierCurve.GetAnchorPoints ();
		Vector2 mousePosition = GetMouseWorldPosition2D ();

		nearBezierPoint = GetNearestControlPointAtMousePosition (bezierPoints, mousePosition, 22.0f);
		nearCurvePoint = GetNearestCurvePointAtMousePosition (curvePoints, mousePosition, 1.0f);
		mouseOverBezierPoint = GetControlPointAtMousePosition (bezierPoints, mousePosition);
		UpdateGrabbedControlPoint (bezierPoints, mousePosition);

		if (nearBezierPoint == null) {
			DrawSceneAddPoint (nearCurvePoint, mousePosition);
		}
		DrawSceneSelectedPoints (nearBezierPoint);

		for (int i = 0; i < bezierPoints.Count; i++) {
			BezierPoint bezierPoint = bezierPoints [i];
			float handleSize = HandleUtility.GetHandleSize (bezierPoint.position) * bezierCurve.handleSize;

			DrawSceneAnchorPointHandle (bezierPoint, nearBezierPoint, mousePosition, handleSize);
			DrawSceneControlPointsHandle (bezierPoint, mousePosition, handleSize);
		}
	}

	private void DrawSceneAddPoint (CurvePoint curvePoint, Vector2 mousePosition)
	{
		if (Event.current.shift) {
			Handles.color = Color.green;
			if (curvePoint != null) {
				Handles.color = Color.blue;
				Handles.DrawSolidDisc (curvePoint.position, Vector3.forward, HandleUtility.GetHandleSize (curvePoint.position) * bezierCurve.handleSize * 0.8f);
			} else {
				Handles.DrawSolidDisc (mousePosition, Vector3.forward, HandleUtility.GetHandleSize (mousePosition) * bezierCurve.handleSize * 0.8f);
			}
			Handles.color = Color.white;
			SceneView.RepaintAll ();

			if (Event.current.type == EventType.mouseDown && Event.current.button == 0) {
				if (curvePoint != null) {
					AddPoint (curvePoint.position, new Vector3 (-2, 0, 0), new Vector3 (2, 0, 0), curvePoint.curveIndex);
				} else {
					AddPoint (new Vector3 (mousePosition.x, mousePosition.y, 0), new Vector3 (-2, 0, 0), new Vector3 (2, 0, 0), 0);
				}
			}
		}
	}

	private void DrawSceneAnchorPointHandle (BezierPoint bezierPoint, BezierPoint nearBezierPoint, Vector2 mousePosition, float handleSize)
	{
		if (selectedPoints.Contains (bezierPoint)) {
			Vector3 p = bezierPoint.GetPosition () - new Vector3 (1, 1, 0) * handleSize;
			Handles.DrawSolidRectangleWithOutline (new Rect (p, Vector3.one * handleSize * 2.0f), Color.yellow, Color.white);
		}

		if (bezierPoint == nearBezierPoint) {
			Handles.color = Color.yellow;
		}

		Vector3 position = Handles.FreeMoveHandle (bezierPoint.GetPosition (), Quaternion.identity, handleSize, Vector3.zero, Handles.RectangleCap);
		Handles.color = Color.white;
		Handles.Label (bezierPoint.GetPosition () + Vector3.right * 0.5f, bezierPoint.name);

		if (bezierPoint.GetLocalPosition () != position) {
			if (Event.current.control) {
				position.x = Mathf.Round (mousePosition.x / bezierCurve.snapSize) * bezierCurve.snapSize;
				position.y = Mathf.Round (mousePosition.y / bezierCurve.snapSize) * bezierCurve.snapSize;
			}

			Undo.RecordObject (target, "Move Point");
			bezierPoint.SetPosition (position);
		}
	}

	private void DrawSceneControlPointsHandle (BezierPoint bezierPoint, Vector2 mousePosition, float handleSize)
	{
		if (bezierPoint.pointType != BezierPointType.None) {
			Vector3 handle1 = Handles.FreeMoveHandle (bezierPoint.GetHandle1Position (), Quaternion.identity, handleSize, Vector3.zero, Handles.SphereCap);
			Vector3 handle2 = Handles.FreeMoveHandle (bezierPoint.GetHandle2Position (), Quaternion.identity, handleSize, Vector3.zero, Handles.SphereCap);

			Handles.DrawLine (bezierPoint.GetPosition (), handle1);
			Handles.DrawLine (bezierPoint.GetPosition (), handle2);

			int handleToAdjust = 0;

			if (bezierPoint.GetHandle1Position () != handle1) {
				if (Event.current.control) {
					handle1.x = Mathf.Round (mousePosition.x / bezierCurve.snapSize) * bezierCurve.snapSize;
					handle1.y = Mathf.Round (mousePosition.y / bezierCurve.snapSize) * bezierCurve.snapSize;
				}

				Undo.RecordObject (target, "Move Handle Point 1");
				bezierPoint.SetHandle1Position (handle1);

				if (bezierPoint.pointType == BezierPointType.Connected) {
					handleToAdjust = 2;
				}
			}
			if (bezierPoint.GetHandle2Position () != handle2) {
				if (Event.current.control) {
					handle2.x = Mathf.Round (mousePosition.x / bezierCurve.snapSize) * bezierCurve.snapSize;
					handle2.y = Mathf.Round (mousePosition.y / bezierCurve.snapSize) * bezierCurve.snapSize;
				}

				Undo.RecordObject (target, "Move Handle Point 2");
				bezierPoint.SetHandle2Position (handle2);

				if (bezierPoint.pointType == BezierPointType.Connected) {
					handleToAdjust = 1;
				}
			}

			if (bezierPoint.pointType == BezierPointType.Connected) {
				if (handleToAdjust == 1) {
					bezierPoint.SetHandle1Position (bezierPoint.GetPosition () + (handle2 - bezierPoint.GetPosition ()) * -1.0f);
				}
				if (handleToAdjust == 2) {
					bezierPoint.SetHandle2Position (bezierPoint.GetPosition () + (handle1 - bezierPoint.GetPosition ()) * -1.0f);
				}
			}

			if (bezierPoint.pointType == BezierPointType.Connected) {
				Color c = new Color (0, 0, 0, 0);
				if (nearBezierPoint == bezierPoint) {
					c = Color.yellow;
				}

				Handles.color = c;
				Quaternion rotation = Handles.Disc (bezierPoint.GetHandlesRotation (), bezierPoint.GetPosition (), Vector3.forward, handleSize * 4.0f, true, 15);
				Handles.color = Color.white;

				if (bezierPoint.GetHandlesRotation () != rotation) {
					Undo.RecordObject (target, "Rotate handle");
					bezierPoint.SetHandlesRotation (rotation);
				}
			}
		}
	}

	private void DrawSceneSelectedPoints (BezierPoint nearBezierPoint)
	{
		if (nearBezierPoint == null) {
			return;
		}

		if (Event.current.type == EventType.mouseDown && Event.current.button == 0) {
			lastSelectedPoint = nearBezierPoint;
		}

		if (Event.current.shift) {
			if (Event.current.type == EventType.mouseDown && Event.current.button == 0) {
				if (!selectedPoints.Contains (nearBezierPoint)) {
					selectedPoints.Add (nearBezierPoint);
				} else {
					selectedPoints.Remove (nearBezierPoint);
				}
			}
		}
	}

	private void NodeTypeShortcuts ()
	{
		Undo.RecordObject (bezierCurve, "Change node type");
		if (lastSelectedPoint != null) {
			if (Event.current.shift) {
				if (Event.current.type == EventType.keyDown) {
					if (Event.current.keyCode == KeyCode.Alpha1) {
						lastSelectedPoint.pointType = BezierPointType.Connected;
						lastSelectedPoint.SetHandlesInConnectedState ();
					}
					if (Event.current.keyCode == KeyCode.Alpha2) {
						lastSelectedPoint.pointType = BezierPointType.Broken;
					}
					if (Event.current.keyCode == KeyCode.Alpha3) {
						lastSelectedPoint.pointType = BezierPointType.None;
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

			if (showSamplePoints) {
				Handles.color = Color.red;
				Handles.DrawSolidDisc (curvePoint.position, Vector3.forward, 0.05f);
				Handles.color = Color.white;
			}
			if (showTangents) {
				Handles.color = Color.blue;
				Handles.DrawLine (curvePoint.position, curvePoint.position + curvePoint.tangent * 5.0f);
				Handles.color = Color.white;
			}
			if (showNormals) {
				Handles.color = Color.green;
				Handles.DrawLine (curvePoint.position, curvePoint.position + curvePoint.normal * 5.0f);
				Handles.color = Color.white;
			}

			if (showCurve) {
				polyLinePoints.Add (curvePoint.position);
			}
		}

		if (showCurve) {
			Handles.DrawPolyLine (polyLinePoints.ToArray ());
		}
	}

	private Vector3 GetSnapSize ()
	{
		return Vector3.one * bezierCurve.snapSize;
	}

	private CurvePoint GetNearestCurvePointAtMousePosition (List<CurvePoint> curvePoints, Vector2 mousePosition, float minDistance)
	{
		for (int i = 0; i < curvePoints.Count; i++) {
			float handleSize = HandleUtility.GetHandleSize (curvePoints [i].position) * bezierCurve.handleSize;
			float pixelDistance = HandleUtility.DistanceToCircle (curvePoints [i].position, handleSize);

			if (pixelDistance < minDistance) {
				return curvePoints [i];
			}
		}

		return null;
	}

	private BezierPoint GetNearestControlPointAtMousePosition (List<BezierPoint> bezierPoints, Vector2 mousePosition, float minDistance)
	{
		for (int i = 0; i < bezierPoints.Count; i++) {
			BezierPoint bezierPoint = bezierPoints [i];
			float handleSize = HandleUtility.GetHandleSize (bezierPoint.position) * bezierCurve.handleSize;
			float pixelDistance = HandleUtility.DistanceToRectangle (bezierPoint.GetPosition (), Quaternion.identity, handleSize);

			if (pixelDistance <= minDistance) {
				return bezierPoint;
			}
		}

		return null;
	}

	private BezierPoint GetControlPointAtMousePosition (List<BezierPoint> bezierPoints, Vector2 mousePosition)
	{
		for (int i = 0; i < bezierPoints.Count; i++) {
			BezierPoint bezierPoint = bezierPoints [i];
			float handleSize = HandleUtility.GetHandleSize (bezierPoint.position) * bezierCurve.handleSize;
			float pixelDistance = HandleUtility.DistanceToRectangle (bezierPoint.GetPosition (), Quaternion.identity, handleSize);

			if (pixelDistance <= 0) {
				return bezierPoint;
			}
		}

		return null;
	}

	private void UpdateGrabbedControlPoint (List<BezierPoint> bezierPoints, Vector2 mousePosition)
	{
		if (Event.current.type == EventType.mouseDown) {
			if (Event.current.button == 0) {
				grabbedBezierPoint = GetControlPointAtMousePosition (bezierPoints, mousePosition);
			}
		}
		if (Event.current.type == EventType.mouseUp) {
			grabbedBezierPoint = null;
		}
	}

	private Vector3 GetMouseWorldPosition ()
	{
		Vector3 mousePosition = Event.current.mousePosition;
		mousePosition.y = Camera.current.pixelHeight - mousePosition.y;
		return Camera.current.ScreenPointToRay (mousePosition).origin;
	}

	private Vector2 GetMouseWorldPosition2D ()
	{
		Vector3 mousePosition = GetMouseWorldPosition ();
		return new Vector2 (mousePosition.x, mousePosition.y);
	}

	private void AddPoint ()
	{
		Undo.RecordObject (bezierCurve, "Add Point");

		Vector3 position = bezierCurve.transform.position + Vector3.up * 5.0f;
		Vector3 handle1 = new Vector3 (-2, 0, 0);
		Vector3 handle2 = new Vector3 (2, 0, 0);

		AddPoint (position, handle1, handle2, 0);
	}

	private void AddPoint (Vector3 position, Vector3 handle1, Vector3 handle2, int curveIndex = 0)
	{
		Undo.RecordObject (bezierCurve, "Add Point");

		BezierPoint bezierPoint = new BezierPoint (position, handle1, handle2);
		bezierCurve.AddAnchorPoint (bezierPoint, curveIndex);
		bezierPoint.SetPosition (position); // wierd code :(

		SceneView.RepaintAll ();
	}

	private void RemoveAllPoints ()
	{
		Undo.RecordObject (bezierCurve, "Remove All Points");

		bezierCurve.RemoveAllAnchorPoints ();
		SceneView.RepaintAll ();
	}

	private void RemovePoint (BezierPoint bezierPoint)
	{
		Undo.RecordObject (bezierCurve, "Remove Point");

		bezierCurve.RemoveAnchorPoint (bezierPoint);
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
