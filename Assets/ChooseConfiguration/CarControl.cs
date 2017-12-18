using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour {

    public bool active = false;
    public GameObject next = null;
    public GameObject prev = null;
    public float angle_velocity = 100.0f;
    public GameObject cameraObj;
    
    bool fix = false;
    int layerMask = 1 << 8;
    bool in_collision = false;

    Camera cameraComp;
    MeshRenderer rendererComp;
    Color initial_color;

    // Use this for initialization
    void Start () {
        cameraComp = (Camera)cameraObj.GetComponent(typeof(Camera));
        rendererComp = ((MeshRenderer)GetComponent(typeof(MeshRenderer)));
        initial_color = rendererComp.material.color;
    }

    void teleportToCursor()
    {
        Ray ray = cameraComp.ScreenPointToRay(Input.mousePosition);
        RaycastHit rh;
        if (Physics.Raycast(ray, out rh, Mathf.Infinity, layerMask))
        {
            Vector3 pt = rh.point;
            pt.y += transform.localScale.y / 2;
            transform.position = pt;
        }
    }

    void switchToNext()
    {
        if (next != null)
        {
            active = false;
            CarControl cc = (CarControl)next.GetComponent(typeof(CarControl));
            cc.Active();
        }
    }
    void switchToPrev()
    {
        if (prev != null)
        {
            active = false;
            rendererComp.enabled = false; // Set invisible
            CarControl cc = (CarControl)prev.GetComponent(typeof(CarControl));
            cc.Active();
        }
    }

    private void OnTriggerEnter(Collider other) { in_collision = true; }
    private void OnTriggerExit(Collider other) { in_collision = false; }

    bool active_next_frame = false;
    public void Active()
    {
        active_next_frame = true;
        rendererComp.enabled = true; // Set visible
        fix = false;
    }

    // Update is called once per frame
    void Update () {
        if (active_next_frame)
        {
            active = true;
            active_next_frame = false;
            return;
        }
        if (!active) 
            return;
        if (!fix)
        {
            teleportToCursor();
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKey(KeyCode.LeftArrow))
                    transform.Rotate(Vector3.up, angle_velocity * Time.fixedDeltaTime);
                if (Input.GetKey(KeyCode.RightArrow))
                    transform.Rotate(Vector3.up, -angle_velocity * Time.fixedDeltaTime);
            }
            float d = Input.GetAxis("Mouse ScrollWheel");
            transform.Rotate(Vector3.up, angle_velocity * d);

            if (in_collision)
                rendererComp.material.color = Color.red;
            else
                rendererComp.material.color = Color.green;

            if (!in_collision && Input.GetMouseButtonDown(0))
            {
                fix = true;
                rendererComp.material.color = initial_color;
                switchToNext();
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (fix == false)
                switchToPrev();
            else
                fix = false;
        }
    }
}
