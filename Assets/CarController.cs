using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {

    public GameObject pivotPoint;
    public GameObject[] wheels;
    public float radius = 5;

    Vector3 wheelsMeanPos;
    float wheelsAngle;
    // Use this for initialization
    void Start () {
        wheelsMeanPos = Vector3.zero;
        wheelsAngle = 0;
        if (wheels.Length > 0)
        {
            foreach (GameObject go in wheels)
                wheelsMeanPos += go.transform.localPosition;
            wheelsMeanPos /= wheels.Length;
            wheelsAngle = (pivotPoint.transform.localPosition - wheelsMeanPos).magnitude;
            wheelsAngle /= 2 * radius;
            wheelsAngle = Mathf.Acos(wheelsAngle) * Mathf.Rad2Deg - 90;
        }
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

    public static Vector3 spatialCoordOfConfiguration(Vector3 conf)
    {
        return new Vector3(conf.x, 0, conf.y);
    }

    public static float normalizeAngle(float angle)
    {
        while (angle < 0)
            angle += 360;
        while (angle >= 360)
            angle -= 360;
        return angle;
    }
    public static Vector3 computeDiffVector(Vector3 init, Vector3 target, bool clockwise)
    {
        Vector3 diff = target - init;
        diff.z = normalizeAngle(diff.z);
        if (!clockwise && diff.z != 0)
            diff.z = diff.z - 360;
        return diff;
    }
    public static float magnitudeOfDiffVector(Vector3 diff)
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

    public void setGlobalScale(float sx, float sy, float sz)
    {
        Vector3 cfg = getConfiguration();
        transform.localScale = new Vector3(sx, sy, sz);
        setConfiguration(cfg);
    }

    public void setWheelsAngle(float angle)
    {
        foreach (GameObject go in wheels)
            go.transform.localEulerAngles = Vector3.up * angle;
    }

    // MOVEMENTS
    Vector3? target = null;
    float remainingTime = 0f;
    bool clockwise = false;
    public void MoveStraigthTo(Vector3 newConf, bool clockwise, float time)
    {
        Vector3 current = getConfiguration();
        if (Mathf.Abs(normalizeAngle(current.z)-normalizeAngle(newConf.z)) <= 0.01f)
            setWheelsAngle(0);
        else
        {
            bool backward = false;
            Vector3 spat_diff = spatialCoordOfConfiguration(computeDiffVector(current, newConf, clockwise));
            Vector3 current_vec = Quaternion.Euler(0, current.z, 0) * Vector3.forward;
            if (Vector3.Dot(spat_diff, current_vec) < 0)
                backward = true;

            if (clockwise == backward)
                setWheelsAngle(wheelsAngle);
            else
                setWheelsAngle(-wheelsAngle);
        }

        target = newConf;
        this.clockwise = clockwise;
        remainingTime = time;
    }
    public void MoveStraigthTo(Vector3 newConf, bool clockwise)
    {
        MoveStraigthTo(newConf, clockwise, magnitudeOfDiffVector(computeDiffVector(getConfiguration(), newConf, clockwise)) * 0.25f);
    }
    public bool MoveFinished()
    {
        return !target.HasValue;
    }
    
	// Update is called once per frame
	void Update () {
		if (target.HasValue)
        {
            Vector3 current = getConfiguration();
            Vector3 diff = computeDiffVector(current, target.Value, clockwise);
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
