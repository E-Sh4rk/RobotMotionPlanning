using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigInfos
{
    public static Vector3 initialPos;
    public static float initialAngle;
    public static Vector3 finalPos;
    public static float finalAngle;
}


public class GUI : MonoBehaviour {

    public GameObject firstCar;
    public GameObject lastCar;

    private void OnGUI()
    {
        if(lastCar.GetComponent<CarControl>().IsFixed())
        {
             if (UnityEngine.GUI.Button(new Rect(Screen.width/2-50, 25, 100, 25), "Go !"))
             {
                ConfigInfos.initialPos = firstCar.GetComponent<CarController>().getPosition();
                ConfigInfos.initialAngle = firstCar.GetComponent<CarController>().getAngle();
                ConfigInfos.finalPos = lastCar.GetComponent<CarController>().getPosition();
                ConfigInfos.finalAngle = lastCar.GetComponent<CarController>().getAngle();
                SceneManager.LoadScene(1);
            }
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
