using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {

    public GameObject pivotPoint;

	// Use this for initialization
	void Start () {

    }

    // CONFIGURATIONS
    public void setAngle(float angle)
    {
        Vector3 pos = pivotPoint.transform.position;
        transform.eulerAngles = Vector3.zero;
        setPosition(new Vector2(pos.x, pos.z));
        transform.RotateAround(pos, Vector3.up, angle);
    }
    public void setPosition(Vector2 pos)
    {
        Vector3 p = new Vector3(pos.x, 0, pos.y);
        transform.position = p - (pivotPoint.transform.position - transform.position);
    }
    public void setConfiguration(Vector3 conf)
    {
        setPosition(new Vector2(conf.x, conf.y)); setAngle(conf.z);
    }
	
    public float getAngle()
    {
        return pivotPoint.transform.eulerAngles.y;
    }
    public Vector2 getPosition()
    {
        return new Vector2(pivotPoint.transform.position.x, pivotPoint.transform.position.z);
    }
    public Vector3 getConfiguration()
    {
        Vector2 pos = getPosition();
        return new Vector3(pos.x, pos.y, getAngle());
    }

    public Vector3 spatialCoordOfConfiguration(Vector3 conf)
    {
        return new Vector3(conf.x, 0, conf.y);
    }

    public float magnitudeOfDiffVector(Vector3 diff)
    {
        return new Vector3(diff.x, diff.y, diff.z * 16f / 360f).magnitude;
    }

    // DISPLAY
    public void changeColor(Color c)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.material.color = c;
    }

    public void setVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }

    // MOVEMENTS
    Vector3? target = null;
    float remainingTime = 0f;
    bool clockwise = false;
    public void MoveStraigthTo(Vector3 newConf, bool clockwise, float time)
    {
        target = newConf;
        this.clockwise = clockwise;
        remainingTime = time;
    }
    public void MoveStraigthTo(Vector3 newConf, bool clockwise)
    {
        MoveStraigthTo(newConf, clockwise, magnitudeOfDiffVector(getConfiguration() - newConf) * 0.1f);
    }
    public bool MoveFinished()
    {
        return !target.HasValue;
    }
    
    public float normalizeAngle(float angle)
    {
        while (angle < 0)
            angle += 360;
        while (angle >= 360)
            angle -= 360;
        return angle;
    }
	// Update is called once per frame
	void Update () {
		if (target.HasValue)
        {
            Vector3 current = getConfiguration();
            Vector3 diff = target.Value - current;
            diff.z = normalizeAngle(diff.z);
            if (!clockwise && diff.z != 0)
                diff.z = diff.z - 360;
            Vector3 new_conf = target.Value;
            if (Time.fixedDeltaTime < remainingTime)
                new_conf = current + diff * (Time.fixedDeltaTime/remainingTime);
            setConfiguration(new_conf);

            remainingTime -= Time.fixedDeltaTime;
            if (remainingTime < 0)
                remainingTime = 0;

            if (new_conf == target.Value)
                target = null;
        }
	}
}
