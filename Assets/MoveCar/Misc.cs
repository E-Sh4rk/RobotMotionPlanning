using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Misc {

	public static Vector3 RSConfToUnityConf(ReedAndShepp.ReedAndShepp.Vector3 v)
    {
        return new Vector3(-v.x,v.y,CarController.normalizeAngle(v.z * Mathf.Rad2Deg + 90));
    }
    public static ReedAndShepp.ReedAndShepp.Vector3 UnityConfToRSConf(Vector3 v)
    {
        return new ReedAndShepp.ReedAndShepp.Vector3(-v.x, v.y, (v.z - 90) * Mathf.Deg2Rad);
    }
    public static Vector3[] RSPathToUnityPath(ReedAndShepp.ReedAndShepp.Vector3[] v)
    {
        Vector3[] p = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
            p[i] = RSConfToUnityConf(v[i]);
        return p;
    }

}
