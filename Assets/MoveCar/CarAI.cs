﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarAI : MonoBehaviour {

    public int maxPointsMonteCarlo = 10000;

    CarController controller;
    OnDemandPhysics phy;
    Bounds bounds;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CarController>();
        phy = GetComponent<OnDemandPhysics>();
        Bounds b = GameObject.Find("Ground").GetComponent<Collider>().bounds;
        bounds = new Bounds(b.center, new Vector3(b.size.x, Mathf.Infinity, b.size.y));

        controller.setConfiguration(ConfigInfos.initialConf);
        if (phy.configurationStraightReachable(ConfigInfos.finalConf))
        {
            targets.Add(ConfigInfos.finalConf);
            targets.Add(ConfigInfos.initialConf);
        }
    }

    struct Link
    {
        public Link(Vector3 c1, Vector3 c2)
        {
            this.c1 = c1;
            this.c2 = c2;
        }
        public Vector3 c1;
        public Vector3 c2;
    }

    float GetMemoisedValue(Dictionary<Link, float> dico, Vector3 c1, Vector3 c2)
    {
        float value = Mathf.Infinity;
        if (!dico.TryGetValue(new Link(c1, c2), out value))
            if (!dico.TryGetValue(new Link(c2, c1), out value))
                return Mathf.Infinity;
        return value;
    }

    List<Vector3> FindPathMonteCarlo()
    {
        /*
        ALGO:
        We draw a point:
          - If it connects two connected component together, we keep it
          - If it is not reachable from any previous configuration, we keep it
          - Otherwise we ignore it
        We update the connected components (union find) and eventually the dico for memoisation
        We stop when the initial config and the final one are in the same connected component
        We retrieve the configurations of this connected component and we search a path with it (we can use a dico for memoisation)
        */
        Dictionary<Link, float> dico = new Dictionary<Link, float>();
        UnionFind<Vector3> components = new UnionFind<Vector3>();
        List<Vector3> pts = new List<Vector3>();

        pts.Add(ConfigInfos.initialConf);
        components.MakeSet(pts[0]);
        pts.Add(ConfigInfos.finalConf);
        components.MakeSet(pts[1]);
        if (phy.configurationsStraightReachable(pts[0], pts[1]))
        {
            dico.Add(new Link(pts[0], pts[1]), (pts[0] - pts[1]).magnitude);
            components.UnionValues(pts[0], pts[1]);
        }

        int i = 0;
        Dictionary<Vector3, bool> tmp_dico = new Dictionary<Vector3, bool>();
        while (components.Find(pts[0]).value != components.Find(pts[1]).value)
        {
            if (i >= maxPointsMonteCarlo)
                return null;

            Vector3 pt = DrawConfiguration();
            if (!phy.configurationInCollision(pt))
            {
                bool reachable = false;
                HashSet<Vector3> components_linked = new HashSet<Vector3>();
                foreach(Vector3 v in pts)
                {
                    if (components_linked.Contains(components.Find(v).value))
                        continue;
                    bool linked = phy.configurationsStraightReachable(v, pt);
                    tmp_dico.Add(v, linked);
                    if (linked)
                    {
                        reachable = true;
                        components_linked.Add(components.Find(v).value);
                    }
                }
                if (!reachable || components_linked.Count >= 2)
                {
                    foreach (Vector3 v in pts)
                    {
                        bool linked = false;
                        try
                        {
                            linked = tmp_dico[v];
                        }
                        catch
                        {
                            linked = phy.configurationsStraightReachable(v, pt);
                        }
                        dico.Add(new Link(v, pt), linked ? (v-pt).magnitude : Mathf.Infinity);
                    }
                    pts.Add(pt);
                    components.MakeSet(pt);
                    foreach (Vector3 v in components_linked)
                        components.UnionValues(pt, v);
                }
                tmp_dico.Clear();
            }
            i++;
        }

        // Searching a path in the connected component of the initial config...
        // TODO

        return null;
    }

    Vector3 DrawConfiguration()
    {
        float x = Random.Range(bounds.center.x - bounds.size.x/2, bounds.center.x + bounds.size.x/2);
        float y = Random.Range(bounds.center.z - bounds.size.z/2, bounds.center.z + bounds.size.z/2);
        float a = Random.Range(0, 360);
        return new Vector3(x,y,a);
    }

    // Update is called once per frame
    List<Vector3> targets = new List<Vector3>();
    void Update () {
        if (phy.inCollisionWithObstacles())
            controller.changeColor(Color.red);
        else
            controller.changeColor(Color.green);

        if (controller.MoveFinished())
        {
            if (targets.Count > 0)
            {
                controller.MoveStraigthTo(targets[0]);
                targets.RemoveAt(0);
            }
        }
    }
}
