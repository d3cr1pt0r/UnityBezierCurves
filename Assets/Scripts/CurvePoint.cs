using UnityEngine;

public class CurvePoint
{

	public Vector3 position;
	public Vector3 normal;
	public Vector3 tangent;

	public int curveIndex;

	public CurvePoint (Vector3 position, Vector3 normal, Vector3 tangent, int curveIndex = 0)
	{
		this.position = position;
		this.normal = normal;
		this.tangent = tangent;

		this.curveIndex = curveIndex;
	}

}
