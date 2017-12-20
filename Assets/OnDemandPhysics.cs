using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnDemandPhysics : MonoBehaviour {

    public float resolution = 1f;

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

    public bool inCollisionWithObstacles()
    {
        Vector3 boxHalfExtents = multiplyComponents(coll.size / 2, transform.localScale);
        Quaternion boxRotation = transform.rotation;
        Vector3 boxCenter = multiplyComponents(coll.center,transform.localScale) + transform.position;

        return Physics.CheckBox(boxCenter, boxHalfExtents, boxRotation, layerMask, QueryTriggerInteraction.Collide);
    }

    public bool configurationStraightReachable(Vector3 init, Vector3 target)
    {
        Vector3 conf_save = control.getConfiguration();

        Vector3 diff = target - init;
        int nb_steps = (int)(diff.magnitude * resolution) + 1;

        bool collision = false;
        for (int i = 0; i <= nb_steps; i++)
        {
            control.setConfiguration(init + diff * ((float)i / nb_steps));
            if (inCollisionWithObstacles())
            {
                collision = true;
                break;
            }
        }

        control.setConfiguration(conf_save);
        return !collision;
    }

    public bool configurationStraightReachable(Vector3 target)
    {
        return configurationStraightReachable(control.getConfiguration(), target);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
