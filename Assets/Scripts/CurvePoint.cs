using UnityEngine;

public class CurvePoint
{

	public Vector3 position;
	public Vector3 normal;
	public Vector3 tangent;

	public CurvePoint (Vector3 position, Vector3 normal, Vector3 tangent)
	{
		this.position = position;
		this.normal = normal;
		this.tangent = tangent;
	}

}
