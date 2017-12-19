﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {

    public GameObject pivotPoint;

	// Use this for initialization
	void Start () {
        setPosition(Vector3.zero);
        setAngle(90);
    }

    public void setAngle(float angle)
    {
        Vector3 pos = pivotPoint.transform.position;
        transform.eulerAngles = Vector3.zero;
        setPosition(pos);
        transform.RotateAround(pos, Vector3.up, angle);
    }
    public void setPosition(Vector3 pos)
    {
        transform.position = pos - pivotPoint.transform.localPosition;
    }
    public void setConfiguration(Vector3 pos, float angle)
    {
        setPosition(pos); setAngle(angle);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
