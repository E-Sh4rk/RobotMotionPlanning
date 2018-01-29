using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Chooser : MonoBehaviour {

    public GameObject cameraObj;
    Camera cameraComp;

    GameObject oldmap;

    int layerMask = 1 << 8;

    // Use this for initialization
    void Start () {
        cameraComp = (Camera)cameraObj.GetComponent(typeof(Camera));
	}
    
    // Call it before loading the map chooser scene
    public static void DestroyMaps()
    {
        foreach (GameObject m in GameObject.FindGameObjectsWithTag("AllMap"))
            Destroy(m);
    }

    // find the maps
    GameObject findTheMap()
    {
        Ray ray = cameraComp.ScreenPointToRay(Input.mousePosition);
        RaycastHit rh;

        GameObject map = null;

        if (Physics.Raycast(ray, out rh, Mathf.Infinity, layerMask))
            map = rh.transform.gameObject;

        return map;
    }


    void colorTheMap(GameObject map)
    {
        Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.material.color = Color.green;
    }

    void decolorTheMap(GameObject map)
    {
        Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.material.color = Color.white;
    }

    GameObject finalMap;

    // Update is called once per frame
    void Update () {

        GameObject map = findTheMap();
        bool selected = map != null;

        if(oldmap != null)
            decolorTheMap(oldmap);

        if (selected)
        {
            colorTheMap(map);
            
            if(Input.GetButtonDown("Validate") && !GUI3.CursorIsOnGUI())
            {
                foreach (GameObject m in GameObject.FindGameObjectsWithTag("AllMap"))
                {
                    DontDestroy g = m.GetComponent<DontDestroy>();
                    if (g.Ground != map)
                        Destroy(m);
                    else
                        finalMap = m;
                }
                decolorTheMap(map);
                finalMap.transform.position = new Vector3(0,-0.75f,0);
                SceneManager.LoadScene(1);
            }

        }

        oldmap = map;
	}
}
