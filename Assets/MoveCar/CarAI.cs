using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarAI : MonoBehaviour {

    CarController controller;
    OnDemandPhysics phy;
    Bounds bounds;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CarController>();
        phy = GetComponent<OnDemandPhysics>();
        Bounds b = GameObject.Find("Ground").GetComponent<Collider>().bounds;
        bounds = new Bounds(b.center, new Vector3(b.size.x, Mathf.Infinity, b.size.y));

        controller.setConfiguration(ConfigInfos.initialConf);
        if (phy.configurationStraightReachable(ConfigInfos.finalConf))
            controller.MoveStraigthTo(ConfigInfos.finalConf, 5f);
    }

    Vector3 DrawConfiguration()
    {
        float x = Random.Range(bounds.center.x - bounds.size.x/2, bounds.center.x + bounds.size.x/2);
        float y = Random.Range(bounds.center.z - bounds.size.z/2, bounds.center.z + bounds.size.z/2);
        float a = Random.Range(0, 360);
        return new Vector3(x,y,a);
    }

    // Update is called once per frame
    void Update () {
        if (phy.inCollisionWithObstacles())
            controller.changeColor(Color.red);
        else
            controller.changeColor(Color.green);
    }
}
