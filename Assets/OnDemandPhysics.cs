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

    public bool moveAllowed(Vector3 init, Vector3 target, bool clockwise)
    {
        Vector3 conf_save = control.getConfiguration();

        Vector3 diff = target - init;
        diff.z = control.normalizeAngle(diff.z);
        if (!clockwise && diff.z != 0)
            diff.z = diff.z - 360;
        int nb_steps = (int)(control.magnitudeOfDiffVector(diff) * resolution) + 1;

        bool collision = false;
        for (int i = 0; i <= nb_steps; i++)
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
    public bool moveAllowed(Vector3 init, Vector3 target)
    {
        return moveAllowed(init, target, false) || moveAllowed(init, target, true);
    }

    public bool clockwisePreferedForMove(Vector3 init, Vector3 target)
    {
        Vector3 diff = target - init;
        diff.z = control.normalizeAngle(diff.z);
        bool result = diff.z < 180;
        if (!moveAllowed(init, target, result) && moveAllowed(init, target, !result))
            return !result;
        return result;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
