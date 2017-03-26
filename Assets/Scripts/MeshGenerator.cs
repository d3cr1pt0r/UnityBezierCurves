using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour
{

	public BezierCurve bezierCurve;

	[SerializeField] private MeshFilter meshFilter;
	[SerializeField] private MeshRenderer meshRenderer;

	private void OnEnable ()
	{
		if (bezierCurve != null) {
			bezierCurve.OnCurveUpdate -= OnCurveUpdate;
			bezierCurve.OnCurveUpdate += OnCurveUpdate;
		}
	}

	private void OnCurveUpdate (List<CurvePoint> curvePoints)
	{
		Mesh mesh = new Mesh ();

		List<Vector3> vertices = new List<Vector3> ();
		List<Vector2> uvs = new List<Vector2> ();
		List<int> triangles = new List<int> ();

		for (int i = 0; i < curvePoints.Count; i++) {
			CurvePoint curvePoint = curvePoints [i];

			Vector3 p0 = curvePoint.position;
			Vector3 p1 = curvePoint.position + curvePoint.normal.normalized * 0.5f;

			vertices.Add (p0);
			vertices.Add (p1);

			if (i > 0) {
				triangles.Add (vertices.Count - 4);
				triangles.Add (vertices.Count - 2);
				triangles.Add (vertices.Count - 3);

				triangles.Add (vertices.Count - 3);
				triangles.Add (vertices.Count - 2);
				triangles.Add (vertices.Count - 1);
			}
		}

		mesh.SetVertices (vertices);
		mesh.SetTriangles (triangles, 0);

		mesh.RecalculateBounds ();
		mesh.RecalculateNormals ();

		meshFilter.mesh = mesh;
	}

}
