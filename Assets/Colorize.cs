using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colorize : MonoBehaviour {

    public Color color;
	// Use this for initialization
	void Start () {
        changeColor(color);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // DISPLAY
    public void changeColor(Color c)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.material.color = c;
    }
}
