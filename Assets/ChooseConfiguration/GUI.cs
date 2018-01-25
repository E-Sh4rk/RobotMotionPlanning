using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigInfos
{
    public static Vector3 initialConf;
    public static Vector3 finalConf;
    public static int mode;
}


public class GUI : MonoBehaviour {

    public GameObject firstCar;
    public GameObject lastCar;

    private void OnGUI()
    {
        if(lastCar.GetComponent<CarControl>().IsFixed())
        {
             if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 75, 25, 150, 25), "Monte Carlo !"))
             {
                ConfigInfos.initialConf = firstCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.finalConf = lastCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.mode = 0;

                SceneManager.LoadScene(2);
            }
            if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 75, 75, 150, 25), "Reed and Shepp !"))
            {
                ConfigInfos.initialConf = firstCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.finalConf = lastCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.mode = 1;

                SceneManager.LoadScene(2);
            }
            if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 75, 125, 150, 25), "Combine both !"))
            {
                ConfigInfos.initialConf = firstCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.finalConf = lastCar.GetComponent<CarController>().getConfiguration();
                ConfigInfos.mode = 2;

                SceneManager.LoadScene(2);
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
