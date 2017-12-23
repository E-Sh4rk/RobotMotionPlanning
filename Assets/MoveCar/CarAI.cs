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
            {
                save_targets = path.ToArray();
                SimplifyPath(save_targets);
                targets.AddRange(save_targets);
            }
            else
                save_targets = null;
        }
        if (ConfigInfos.mode == 1)
        {
            ReedAndShepp.ReedAndShepp.Vector3[] path;
            Debug.Log(ras.ComputeCurve(Misc.UnityConfToRSConf(ConfigInfos.initialConf), Misc.UnityConfToRSConf(ConfigInfos.finalConf), 0.1, out path));
            save_targets = Misc.RSPathToUnityPath(path);
            targets.AddRange(save_targets);
        }
        if (ConfigInfos.mode == 2)
        {
            List<Vector3> path = FindPath();
            if (path != null)
            {
                Vector3[] mc_path = path.ToArray();
                SimplifyPath(mc_path);
                path = new List<Vector3>();

                for (int i = 1; i < mc_path.Length; i++)
                {
                    Vector3[] p = ComputeRASOfStraightLine(mc_path[i-1], mc_path[i], phy.clockwisePreferedForMove(mc_path[i-1], mc_path[i]));
                    path.AddRange(p);
                }

                save_targets = path.ToArray();
                targets.AddRange(save_targets);
            }
            else
                save_targets = null;
        }
    }
    Vector3[] save_targets = null;

    public void replay()
    {
        finished = false;
        controller.setConfiguration(ConfigInfos.initialConf);
        if (save_targets != null)
            targets.AddRange(save_targets);
    }

    Vector3[] ComputeRASOfStraightLine(Vector3 init, Vector3 target, bool clockwise)
    {
        ReedAndShepp.ReedAndShepp.Vector3[] ras_path;
        ras.ComputeCurve(Misc.UnityConfToRSConf(init), Misc.UnityConfToRSConf(target), 0.1, out ras_path);
        Vector3[] path = Misc.RSPathToUnityPath(ras_path);
        if (phy.pathAllowed(path))
            return path;
        else
        {
            Vector3 middle_conf = init + CarController.computeDiffVector(init, target, clockwise)/2;
            Vector3[] path1 = ComputeRASOfStraightLine(init, middle_conf, clockwise);
            Vector3[] path2 = ComputeRASOfStraightLine(middle_conf, target, clockwise);
            path = new Vector3[path1.Length + path2.Length - 1];
            System.Array.Copy(path1, path, path1.Length);
            System.Array.Copy(path2, 1, path, path1.Length, path2.Length-1);
            return path;
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
    
    float distanceBetweenConf(Vector3 v1, Vector3 v2)
    {
        return CarController.magnitudeOfDiffVector(CarController.computeDiffVector(v1, v2, phy.clockwisePreferedForMove(v1, v2)));
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
            dico.Add(new Link(pts[0], pts[1]), distanceBetweenConf(pts[0],pts[1]));
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
                        dico.Add(new Link(v, pt), linked ? distanceBetweenConf(v, pt) : Mathf.Infinity);
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
    void SimplifyPath(Vector3[] p)
    {
        // TODO : Real simplification
        // For example : keep away conf from the obstacles by growing the car and see where the collision is. We do this until the move become impossible.
        for (int i = 1; i < p.Length - 1; i++)
        {
            // Optimisation : adapt the rotation of the target to a more natural one
            Vector3 orientation = CarController.spatialCoordOfConfiguration(p[i+1]) - CarController.spatialCoordOfConfiguration(p[i-1]);
            float angle = Vector3.Angle(new Vector3(0, 0, 1), orientation);
            if (orientation.x < 0)
                angle = 360 - angle;
            Vector3 new_target = new Vector3(p[i].x, p[i].y, angle);
            if (phy.moveAllowed(p[i - 1], new_target) && phy.moveAllowed(new_target, p[i + 1]))
                p[i] = new_target;
        }
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
