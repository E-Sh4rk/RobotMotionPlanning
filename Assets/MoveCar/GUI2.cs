using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GUI2 : MonoBehaviour {

    public GameObject car;

    private void OnGUI()
    {
        if (car.GetComponent<CarAI>().HasFinished())
        {
            if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 50, 25, 100, 25), "Back"))
            {
                Chooser.DestroyMaps();
                SceneManager.LoadScene(0);
            }
            if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 50, 75, 100, 25), "Replay"))
                car.GetComponent<CarAI>().replay();
            if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 50, 125, 100, 25), "Retry"))
                car.GetComponent<CarAI>().retry();
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
