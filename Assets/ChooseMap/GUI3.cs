using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI3 : MonoBehaviour {

    public static float radius_val = 5f;

    private void OnGUI()
    {
        UnityEngine.GUI.Label(new Rect(Screen.width / 2 - 125, 20, 50, 20), "Radius");
        radius_val = UnityEngine.GUI.HorizontalSlider(new Rect(Screen.width / 2 - 75, 25, 150, 25), radius_val, 1.0F, 10.0F);
        if (UnityEngine.GUI.Button(new Rect(Screen.width / 2 - 75, 50, 150, 25), "Quit"))
        {
            Application.Quit();
        }
    }

    public static bool CursorIsOnGUI()
    {
        Vector2 coord = GUIUtility.ScreenToGUIPoint(Input.mousePosition);
        coord.y = Screen.height - coord.y;
        return new Rect(Screen.width / 2 - 75, 25, 150, 50).Contains(coord);
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
