using System.Collections.Generic;
using UnityEngine;

public class BezierCurveData : ScriptableObject
{

	public List<BezierPoint> points;
	public bool connectedCurve;
	public float handleSize;
	public float snapSize;
	public int sampleRate;

}
