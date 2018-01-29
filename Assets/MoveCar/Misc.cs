using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Misc {

	public static Vector3 RSConfToUnityConf(ReedAndShepp.ReedAndShepp.Vector3 v)
    {
        return new Vector3((float)-v.x,(float)v.y,CarController.normalizeAngle((float)(v.z * Mathf.Rad2Deg + 90)));
    }
    public static ReedAndShepp.ReedAndShepp.Vector3 UnityConfToRSConf(Vector3 v)
    {
        return new ReedAndShepp.ReedAndShepp.Vector3(-v.x, v.y, (v.z - 90.0) * Mathf.Deg2Rad);
    }
    public static Vector3[] RSPathToUnityPath(ReedAndShepp.ReedAndShepp.Vector3[] v, Vector3 init, Vector3 target)
    {
        if (v.Length == 0)
            return new Vector3[] { init, target };
        List<Vector3> p = new List<Vector3>();
        for (int i = 0; i < v.Length; i++)
            p.Add(RSConfToUnityConf(v[i]));
        if (p[0] != init)
            p.Insert(0, init);
        if (p[p.Count-1] != target || p.Count < 2)
            p.Add(target);
        return p.ToArray();
    }

}
