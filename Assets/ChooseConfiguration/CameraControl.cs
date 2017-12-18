using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

    public float velocity = 10.0f;
	
	// Update is called once per frame
	void Update () {
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                this.transform.Translate(Vector3.left * velocity * Time.fixedDeltaTime);
            if (Input.GetKey(KeyCode.RightArrow))
                this.transform.Translate(Vector3.right * velocity * Time.fixedDeltaTime);
            if (Input.GetKey(KeyCode.UpArrow))
                this.transform.Translate(Vector3.up * velocity * Time.fixedDeltaTime);
            if (Input.GetKey(KeyCode.DownArrow))
                this.transform.Translate(Vector3.down * velocity * Time.fixedDeltaTime);
        }
        
    }
}
