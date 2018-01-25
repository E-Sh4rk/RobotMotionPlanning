using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	// Use this for initialization
	void Start () {
        goToPos();
	}

    public float velocity = 10.0f;
    public int currentPos = 0;

    void goToPos()
    {
        switch (currentPos)
        {
            case 0:
                transform.position = new Vector3(0,25,0);
                transform.rotation = Quaternion.Euler(90,0,0);
                break;
            case 1:
                transform.position = new Vector3(-15, 15, 15);
                transform.rotation = Quaternion.Euler(45, 135, 0);
                break;
            case 2:
                transform.position = new Vector3(15, 15, -15);
                transform.rotation = Quaternion.Euler(145, 135, 180);
                break;
        }
    }

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
        if (Input.GetKey(KeyCode.PageDown) || Input.GetKey(KeyCode.M))
            this.transform.Translate(Vector3.forward * velocity * Time.fixedDeltaTime);
        if (Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.L))
            this.transform.Translate(Vector3.back * velocity * Time.fixedDeltaTime);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPos = (currentPos + 1) % 3;
            goToPos();
        }
    }
}
