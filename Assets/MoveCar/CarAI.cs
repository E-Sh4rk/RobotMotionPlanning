using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour {

    CarController controller;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CarController>();
        controller.setConfiguration(ConfigInfos.initialConf);
        controller.MoveStraigthTo(ConfigInfos.finalConf, 5f);
	}

	// Update is called once per frame
	void Update () {
		
	}
}
