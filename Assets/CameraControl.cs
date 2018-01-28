using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	// Use this for initialization
	void Start () {
        if (!lastPos.HasValue)
            goToPos();
        else
        {
            transform.position = lastPos.Value;
            transform.rotation = lastRot.Value;
        }
	}

    public float velocity = 10.0f;

    static int currentPos = 0;
    static Vector3? lastPos = null;
    static Quaternion? lastRot = null;
    public static bool headlights = true;

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
        this.transform.Translate(Vector3.right * velocity * Input.GetAxis("Camera Horizontal"));
        this.transform.Translate(Vector3.up * velocity * Input.GetAxis("Camera Vertical"));
        this.transform.Translate(Vector3.forward * velocity * Input.GetAxis("Camera Zoom"));
        if (Input.GetButtonDown("Camera View"))
        {
            currentPos = (currentPos + 1) % 3;
            goToPos();
        }
        lastPos = transform.position;
        lastRot = transform.rotation;
        if (Input.GetButtonDown("Headlights"))
            headlights = !headlights;
    }
}
