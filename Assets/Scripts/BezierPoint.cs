using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BezierPoint
{
	[SerializeField] public string name;
	[SerializeField] public BezierPointType pointType;
	[SerializeField] public Vector3 position;
	[SerializeField] public Vector3 handle1;
	[SerializeField] public Vector3 handle2;

	[SerializeField] private BezierCurve bezierCurve;

	public BezierPoint (Vector3 position, Vector3 handle1, Vector3 handle2, string name = "Point")
	{
		this.name = name;
		this.position = position;
		this.handle1 = handle1;
		this.handle2 = handle2;
		this.pointType = BezierPointType.None;
	}

	public Vector3 GetPosition ()
	{
		return position + bezierCurve.transform.position;
	}

	public Vector3 GetHandle1Position ()
	{
		return handle1 + GetPosition ();
	}

	public Vector3 GetHandle2Position ()
	{
		return handle2 + GetPosition ();
	}

	public Vector3 GetLocalPosition ()
	{
		return position;
	}

	public Vector3 GetHandle1LocalPosition ()
	{
		return handle1;
	}

	public Vector3 GetHandle2LocalPosition ()
	{
		return handle2;
	}

	public Quaternion GetHandlesRotation ()
	{
		return Quaternion.LookRotation (GetHandle2Position () - GetPosition ());
	}

	public void SetHandlesRotation (Quaternion rotation)
	{
		Vector3 dir = (rotation * Vector3.forward).normalized;
		float handleMagnitude = (GetHandle2Position () - GetPosition ()).magnitude;
		SetHandle2Position (GetPosition () + dir * handleMagnitude);
		SetHandle1Position (GetPosition () - dir * handleMagnitude);
	}

	public void SetPosition (Vector3 position)
	{
		this.position = position - bezierCurve.transform.position;
	}

	public void SetHandle1Position (Vector3 position)
	{
		handle1 = position - GetPosition ();
	}

	public void SetHandle2Position (Vector3 position)
	{
		handle2 = position - GetPosition ();
	}

	public void SetToCurve (BezierCurve bezierCurve)
	{
		this.bezierCurve = bezierCurve;
	}

}
