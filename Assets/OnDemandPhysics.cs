using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnDemandPhysics : MonoBehaviour {

    public float resolution = 3f;

    BoxCollider coll;
    CarController control;
    int layerMask = 1 << 9;

    // Use this for initialization
    void Start () {
        coll = GetComponent<BoxCollider>();
        control = GetComponent<CarController>();
    }

    Vector3 multiplyComponents(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

    public bool configurationInCollision(Vector3 conf)
    {
        Vector3 conf_save = control.getConfiguration();
        control.setConfiguration(conf);
        bool result = currentlyInCollision();
        control.setConfiguration(conf_save);
        return result;
    }
    public bool currentlyInCollision()
    {
        Vector3 boxHalfExtents = multiplyComponents(coll.size / 2, transform.localScale);
        Quaternion boxRotation = transform.rotation;
        Vector3 boxCenter = boxRotation * multiplyComponents(coll.center,transform.localScale) + transform.position;

        return Physics.CheckBox(boxCenter, boxHalfExtents, boxRotation, layerMask, QueryTriggerInteraction.Collide);
    }

    public bool moveAllowed(Vector3 init, Vector3 target, bool clockwise, bool test_init = true)
    {
        Vector3 conf_save = control.getConfiguration();

        Vector3 diff = CarController.computeDiffVector(init, target, clockwise);
        int nb_steps = (int)(CarController.magnitudeOfDiffVector(diff) * resolution) + 1;

        bool collision = false;
        for (int i = test_init?0:1; i <= nb_steps; i++)
        {
            control.setConfiguration(init + diff * ((float)i / nb_steps));
            if (currentlyInCollision())
            {
                collision = true;
                break;
            }
        }

        control.setConfiguration(conf_save);
        return !collision;
    }
    public bool moveAllowed(bool ras, Vector3 init, Vector3 target, bool test_init = true)
    {
        if (ras)
            return moveAllowed(init, target, clockwiseForRASMove(init, target), test_init);
        else
            return moveAllowed(init, target, false, test_init) || moveAllowed(init, target, true, test_init);
    }
    public bool pathAllowed(bool ras, Vector3[] path)
    {
        if (path.Length == 1)
            if (configurationInCollision(path[0]))
                return false;
        for (int i = 1; i < path.Length; i++)
        {
            if (!moveAllowed(ras, path[i - 1], path[i], i==1))
                return false;
        }
        return true;
    }

    public bool clockwisePreferedForMove(Vector3 init, Vector3 target)
    {
        bool result = clockwiseForRASMove(init, target);
        if (!moveAllowed(init, target, result) && moveAllowed(init, target, !result))
            return !result;
        return result;
    }
    public bool clockwiseForRASMove(Vector3 init, Vector3 target)
    {
        Vector3 diff = target - init;
        diff.z = CarController.normalizeAngle(diff.z);
        return diff.z < 180;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
