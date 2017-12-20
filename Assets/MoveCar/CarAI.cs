using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour {

    CarController controller;
    OnDemandPhysics phy;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CarController>();
        phy = GetComponent<OnDemandPhysics>();
        controller.setConfiguration(ConfigInfos.initialConf);
        controller.MoveStraigthTo(ConfigInfos.finalConf, 5f);
	}

	// Update is called once per frame
	void Update () {
        if (phy.inCollisionWithObstacles())
            controller.changeColor(Color.red);
        else
            controller.changeColor(Color.green);
	}
}
