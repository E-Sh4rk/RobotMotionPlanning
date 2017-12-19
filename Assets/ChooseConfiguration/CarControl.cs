using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour {

    public bool active = false;
    public GameObject next = null;
    public GameObject prev = null;
    public float angle_velocity = 100.0f;
    public GameObject cameraObj;
    public Color initial_color;

    bool fix = false;
    int layerMask = 1 << 8;
    bool in_collision = false;
    Camera cameraComp;

    // Use this for initialization
    void Start () {
        cameraComp = (Camera)cameraObj.GetComponent(typeof(Camera));
        changeColor(initial_color);
        if (!active)
            setVisible(false);
    }

    void changeColor(Color c)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material.color = c; 
        }
    }

    void setVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }

    void teleportToCursor()
    {
        Ray ray = cameraComp.ScreenPointToRay(Input.mousePosition);
        RaycastHit rh;
        if (Physics.Raycast(ray, out rh, Mathf.Infinity, layerMask))
        {
            Vector3 pt = rh.point;
            //pt.y += GetComponent<Collider>().bounds.size.y/2;
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
            setVisible(false);
            CarControl cc = (CarControl)prev.GetComponent(typeof(CarControl));
            cc.Active();
        }
    }

    private void OnTriggerEnter(Collider other) { in_collision = true; }
    private void OnTriggerStay(Collider other) { in_collision = true; }
    private void OnTriggerExit(Collider other) { in_collision = false; }

    bool active_next_frame = false;
    public void Active()
    {
        active_next_frame = true;
        setVisible(true);
        fix = false;
    }

    public bool IsFixed() { return fix; }

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
                changeColor(Color.red);
            else
                changeColor(Color.green);

            if (!in_collision && Input.GetMouseButtonDown(0))
            {
                fix = true;
                changeColor(initial_color);
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
