using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroy : MonoBehaviour {

    public GameObject Ground;

    private void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    public GameObject getGround()
    {
        Debug.Log("test");
        return Ground;
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

}