using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarAI : MonoBehaviour {

    public int maxPointsMonteCarlo = 2000;
    public int maxConsecutiveRejections = 10;

    CarController controller;
    OnDemandPhysics phy;
    Bounds bounds;
    ReedAndShepp.ReedAndShepp ras;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CarController>();
        phy = GetComponent<OnDemandPhysics>();
        Bounds b = GameObject.Find("Ground").GetComponent<Collider>().bounds;
        bounds = new Bounds(b.center, new Vector3(b.size.x, Mathf.Infinity, b.size.z));
        ras = new ReedAndShepp.ReedAndShepp(10, Application.streamingAssetsPath);

        controller.setConfiguration(ConfigInfos.initialConf);

        if (ConfigInfos.mode == 0)
        {
            List<Vector3> path = FindPath();
            if (path != null)
                targets.AddRange(path);
        }
        if (ConfigInfos.mode == 1)
        {
            ReedAndShepp.ReedAndShepp.Vector3[] tmp;
            Debug.Log(ras.ComputeCurve(Misc.UnityConfToRSConf(ConfigInfos.initialConf), Misc.UnityConfToRSConf(ConfigInfos.finalConf), 0.1, out tmp));
            targets.AddRange(Misc.RSPathToUnityPath(tmp));
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
        Please configure the random generation of configurations before calling this function (or you can use FindPath instead).
        ALGO:
        We draw a point:
          - If it connects two connected component together, we keep it
          - If it is not reachable from any previous configuration, we keep it
          - Otherwise we ignore it
          - We can also accept a point that do not respect any of the conditions after a certain number of consecutive rejections
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
        if (phy.moveAllowed(pts[0], pts[1]))
        {
            dico.Add(new Link(pts[0], pts[1]), CarController.spatialCoordOfConfiguration(pts[0] - pts[1]).magnitude);
            components.UnionValues(pts[0], pts[1]);
        }

        int i = 0;
        int cons_rejections = 0;
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
                    bool linked = phy.moveAllowed(v, pt);
                    tmp_dico.Add(v, linked);
                    if (linked)
                    {
                        reachable = true;
                        components_linked.Add(components.Find(v).value);
                    }
                }
                if (!reachable || components_linked.Count >= 2 || cons_rejections >= maxConsecutiveRejections)
                {
                    cons_rejections = 0;
                    foreach (Vector3 v in pts)
                    {
                        bool linked = false;
                        try
                        {
                            linked = tmp_dico[v];
                        }
                        catch
                        {
                            linked = phy.moveAllowed(v, pt);
                        }
                        if (linked)
                            Debug.DrawLine(CarController.spatialCoordOfConfiguration(v), CarController.spatialCoordOfConfiguration(pt), Color.red, 5f);
                        dico.Add(new Link(v, pt), linked ? CarController.spatialCoordOfConfiguration(v-pt).magnitude : Mathf.Infinity);
                    }
                    pts.Add(pt);
                    components.MakeSet(pt);
                    foreach (Vector3 v in components_linked)
                        components.UnionValues(pt, v);
                }
                else
                    cons_rejections++;
                tmp_dico.Clear();
                i++;
            }
        }

        // Searching a path with Dijkstra in the connected component of the initial config...
        Vector3 component = components.Find(ConfigInfos.initialConf).value;
        pts.RemoveAll((v => components.Find(v).value != component));
        Priority_Queue.SimplePriorityQueue<Vector3> vertices = new Priority_Queue.SimplePriorityQueue<Vector3>();
        foreach (Vector3 pt in pts)
        {
            if (pt == ConfigInfos.initialConf)
                vertices.Enqueue(pt, 0);
            else
                vertices.Enqueue(pt, Mathf.Infinity);
        }

        Dictionary<Vector3, Vector3> paths = new Dictionary<Vector3, Vector3>();
        float current_w = vertices.GetPriority(vertices.First);
        Vector3 current = vertices.Dequeue();
        while (current != ConfigInfos.finalConf)
        {
            foreach(Vector3 v in vertices)
            {
                float w = GetMemoisedValue(dico, v, current);
                float new_w = w + current_w;
                if (new_w < vertices.GetPriority(v))
                {
                    vertices.UpdatePriority(v, new_w);
                    paths[v] = current;
                }
            }
            current_w = vertices.GetPriority(vertices.First);
            current = vertices.Dequeue();
        }

        List<Vector3> result = new List<Vector3>();
        result.Add(ConfigInfos.finalConf);
        current = ConfigInfos.finalConf;
        while (current != ConfigInfos.initialConf)
        {
            current = paths[current];
            result.Add(current);
        }
        result.Reverse();
        return result;
    }

    List<Vector3> FindPath()
    {
        // Try 1 : the manathan way
        random_allowed_angles = new int[] { 0, 90, 180, 270 };
        List<Vector3> path = FindPathMonteCarlo();
        if (path != null)
            return path;
        Debug.Log("Manathan search failed.");
        // Try 2 : intermediate way
        random_allowed_angles = new int[] { 0, 45, 90, 135, 180, 225, 270, 315 };
        path = FindPathMonteCarlo();
        if (path != null)
            return path;
        Debug.Log("Intermediate search failed.");
        // Try 3 : full configurations space
        random_allowed_angles = null;
        path = FindPathMonteCarlo();
        if (path != null)
            return path;
        Debug.Log("Full search failed.");

        return null;
    }

    int[] random_allowed_angles = null;
    Vector3 DrawConfiguration()
    {
        float x = Random.Range(bounds.center.x - bounds.size.x/2, bounds.center.x + bounds.size.x/2);
        float y = Random.Range(bounds.center.z - bounds.size.z/2, bounds.center.z + bounds.size.z/2);
        int a = 0;
        if (random_allowed_angles == null)
            a = Random.Range(0, 360);
        else
            a = random_allowed_angles[Random.Range(0, random_allowed_angles.Length)];

        return new Vector3(x,y,a);
    }

    bool finished = false;
    public bool HasFinished()
    {
        return finished;
    }

    // Update is called once per frame
    List<Vector3> targets = new List<Vector3>();
    void Update () {
        if (phy.currentlyInCollision())
            controller.changeColor(Color.red);
        else
            controller.changeColor(Color.green);

        if (controller.MoveFinished())
        {
            if (targets.Count > 0)
            {
                controller.MoveStraigthTo(targets[0], phy.clockwisePreferedForMove(controller.getConfiguration(),targets[0]));
                targets.RemoveAt(0);
            }
            else
                finished = true;
        }
    }
}
