using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigInfos
{
    public static Vector3 initialConf;
    public static Vector3 finalConf;
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
                ConfigInfos.initialConf = firstCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.finalConf = lastCar.GetComponent<CarController>().getConfiguration();

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
