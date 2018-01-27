using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    CarController control;

    // Use this for initialization
    void Start () {
        cameraComp = (Camera)cameraObj.GetComponent(typeof(Camera));
        control = GetComponent<CarController>();
        control.changeColor(initial_color);
        if (!active)
            control.setVisible(false);
    }

    void teleportToCursor()
    {
        Ray ray = cameraComp.ScreenPointToRay(Input.mousePosition);
        RaycastHit rh;
        if (Physics.Raycast(ray, out rh, Mathf.Infinity, layerMask))
        {
            Vector3 pt = rh.point;
            pt.y = 0;
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
            control.setVisible(false);
            CarControl cc = (CarControl)prev.GetComponent(typeof(CarControl));
            cc.Active();
        }
        else
        {
            Chooser.DestroyMaps();
            SceneManager.LoadScene(0);
        }
    }

    private void OnTriggerEnter(Collider other) { in_collision = true; }
    private void OnTriggerStay(Collider other) { in_collision = true; }
    private void OnTriggerExit(Collider other) { in_collision = false; }

    bool active_next_frame = false;
    public void Active()
    {
        active_next_frame = true;
        control.setVisible(true);
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

            transform.Rotate(Vector3.up, angle_velocity * Input.GetAxis("Car Rotation"));
            transform.Rotate(Vector3.up, angle_velocity * Input.GetAxis("Mouse ScrollWheel"));

            // in_collision = GetComponent<OnDemandPhysics>().inCollisionWithObstacles();
            if (in_collision)
                control.changeColor(Color.red);
            else
                control.changeColor(Color.green);

            if (!in_collision && Input.GetButtonDown("Validate"))
            {
                fix = true;
                control.changeColor(initial_color);
                switchToNext();
            }
        }
        if (Input.GetButtonDown("Cancel"))
        {
            if (fix == false)
                switchToPrev();
            else
                fix = false;
        }
    }
}
