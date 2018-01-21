using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarAI : MonoBehaviour {

    public int maxPointsMonteCarlo = 2000;
    public int minPointsMonteCarlo = 500;
    public int maxConsecutiveRejections = 10;
    public int rasMaxDepth = 7;
    public int rasApproxDepth = 1;
    public float rasMaxCutsPerUnit = 0.5f;

    CarController controller;
    OnDemandPhysics phy;
    Bounds bounds;
    ReedAndShepp.ReedAndShepp ras;

    // Use this for initialization
    void Start() {
        controller = GetComponent<CarController>();
        phy = GetComponent<OnDemandPhysics>();
        Bounds b = GameObject.Find("Ground").GetComponent<Collider>().bounds;
        bounds = new Bounds(b.center, new Vector3(b.size.x, Mathf.Infinity, b.size.z));
        try
        {
            ReedAndShepp.ReedAndShepp.SetDllFolder(Application.streamingAssetsPath);
            ras = new ReedAndShepp.ReedAndShepp(controller.radius);
        }
        catch
        {
            Debug.Log("Error while initializing R&S module !");
        }

        retry();
    }
    Vector3[] save_targets = null;

    public void replay()
    {
        finished = false;
        controller.setConfiguration(ConfigInfos.initialConf);
        if (save_targets != null)
            targets.AddRange(save_targets);
    }

    public void retry()
    {
        if (ras == null)
            return;
        finished = false;
        controller.setConfiguration(ConfigInfos.initialConf);

        controller.setConfiguration(ConfigInfos.initialConf);

        if (ConfigInfos.mode == 0)
        {
            List<Vector3> path = FindPath();
            if (path != null)
            {
                save_targets = path.ToArray();
                targets.AddRange(save_targets);
            }
            else
                save_targets = null;
        }
        if (ConfigInfos.mode == 1)
        {
            ReedAndShepp.ReedAndShepp.Vector3[] path;
            ras.ComputeCurve(Misc.UnityConfToRSConf(ConfigInfos.initialConf), Misc.UnityConfToRSConf(ConfigInfos.finalConf), 0.1, out path);
            save_targets = Misc.RSPathToUnityPath(path);
            targets.AddRange(save_targets);
        }
        if (ConfigInfos.mode == 2)
        {
            List<Vector3> path = FindPath();
            if (path != null)
            {
                if (FindRasPath(path.ToArray(), out save_targets) >= Mathf.Infinity)
                {
                    Debug.Log("R&S depth exceeded !");
                    save_targets = null;
                }
                else
                    targets.AddRange(save_targets);
            }
            else
                save_targets = null;
        }
    }

    float FindRasPath(Vector3[] path, out Vector3[] save_targets)
    {
        ras_cuts_per_unit = 0;
        float res = ComputeOptimizedRAS(path, rasMaxDepth, rasMaxDepth, out save_targets);
        if (res < Mathf.Infinity)
            return res;
        ras_cuts_per_unit = rasMaxCutsPerUnit;
        return ComputeOptimizedRAS(path, rasMaxDepth, rasMaxDepth, out save_targets);
    }

    float ComputeOptimizedRAS(Vector3[] p, int max_depth, int opti_max_depth, out Vector3[] opt_path/*, int middle*/)
    {
        int middle = p.Length-1;
        List<Vector3> path = new List<Vector3>();
        float len = 0;
        Vector3[] tmp_val;

        Vector3 last = p[0];
        path.Add(p[0]);
        for (int i = 1; i < middle; i++)
        {
            float tmp_len = OptimizedRASofLine(last, p[i], p[i+1], max_depth, opti_max_depth, out tmp_val);
            if (tmp_len >= Mathf.Infinity)
            {
                middle = i;
                break;
            }
            len += tmp_len;
            for (int j = 1; j < tmp_val.Length; j++)
                path.Add(tmp_val[j]);
            last = tmp_val[tmp_val.Length-1];
        }

        List<Vector3> path_rev = new List<Vector3>();
        last = p[p.Length - 1];
        path_rev.Add(p[p.Length-1]);
        for (int i = p.Length - 2; i > middle; i--)
        {
            len += OptimizedRASofLine(last, p[i], p[i-1], max_depth, opti_max_depth, out tmp_val);
            for (int j = 1; j < tmp_val.Length; j++)
                path_rev.Add(tmp_val[j]);
            last = tmp_val[tmp_val.Length - 1];
        }
        path_rev.Reverse();

        if (middle > 0 && middle < p.Length-1)
        {
            len += OptimizedRASofLine(path[path.Count - 1], p[middle], path_rev[0], max_depth, opti_max_depth, out tmp_val);
            for (int j = 1; j < tmp_val.Length; j++)
                path.Add(tmp_val[j]);
        }
        len += RASofLine(path[path.Count - 1], path_rev[0], max_depth, opti_max_depth, out tmp_val);
        for (int j = 1; j < tmp_val.Length; j++)
            path.Add(tmp_val[j]);

        path.AddRange(path_rev);
        opt_path = path.ToArray();
        return len;
    }

    // Give an optimized path between p1 and p2. p3 is the next point.
    // If going from p2 to p3 is impossible anyway, the len returned will always be infinity even if the len from p1 to p2 is finite.
    float OptimizedRASofLine(Vector3 p1, Vector3 p2, Vector3 p3, int max_depth, int opti_max_depth, out Vector3[] output)
    {
        Vector3 tmp_conf;
        float len = 0;
        if (opti_max_depth <= 0)
        {
            // No point optimization
            len = RASofLine(p1, p2, max_depth, opti_max_depth, out output);
        }
        else
        {
            // Point optimization
            CostFunc cost = (Vector3 v) =>
            {
                Vector3[] devnull;
                return RASofLine(p1, v, max_depth, Mathf.Min(rasApproxDepth, opti_max_depth), out devnull)
                + RASofLine(v, p3, max_depth, Mathf.Min(rasApproxDepth, opti_max_depth), out devnull);
            };
            len = optimizePoint(p2, cost, out tmp_conf);
            if (len < Mathf.Infinity) // The len that interest us is only the len from p1 to p2
                len = RASofLine(p1, tmp_conf, max_depth, opti_max_depth, out output);
            else
                output = new Vector3[] { p1, p2 };
        }
        return len;
    }

    delegate float CostFunc(Vector3 conf);
    const float delta = 0.5f;
    const float angle_delta = 30f;
    const bool test_all_angles_in_one_iteration = true;
    const bool use_directly_small_step = false;
    const float small_delta = 0.1f;
    const float small_angle_delta = 5f;
    const bool small_test_all_angles_in_one_iteration = false;
    float optimizePoint(Vector3 conf, CostFunc cost, out Vector3 conf_min, bool smallStep = use_directly_small_step)
    {
        float delta = CarAI.delta;
        float angle_delta = CarAI.angle_delta;
        bool test_all_angles_in_one_iteration = CarAI.test_all_angles_in_one_iteration;
        if (smallStep)
        {
            delta = CarAI.small_delta;
            angle_delta = CarAI.small_angle_delta;
            test_all_angles_in_one_iteration = CarAI.small_test_all_angles_in_one_iteration;
        }

        // Compute all possible adjacent conf
        Vector3[] r2_pos = new Vector3[] { conf + new Vector3(delta, 0, 0), conf + new Vector3(-delta, 0, 0),
        conf + new Vector3(0, delta, 0), conf + new Vector3(0, -delta, 0) };
        List<Vector3> all_pos = new List<Vector3>(r2_pos);
        if (test_all_angles_in_one_iteration)
        {
            int nb = Mathf.CeilToInt(360 / angle_delta);
            for (int i = 1; i < nb; i++)
            {
                float angle = CarController.normalizeAngle(conf.z + i * angle_delta);
                all_pos.Add(new Vector3(conf.x, conf.y, angle));
            }
        }
        else
        {
            float angle = CarController.normalizeAngle(conf.z + angle_delta);
            all_pos.Add(new Vector3(conf.x, conf.y, angle));
            angle = CarController.normalizeAngle(conf.z - angle_delta);
            all_pos.Add(new Vector3(conf.x, conf.y, angle));
        }
        // Remove those in collision
        all_pos.RemoveAll((c => phy.configurationInCollision(c)));
        // Compute best position
        float min = cost(conf);
        Vector3? min_conf = null;
        foreach (Vector3 v in all_pos)
        {
            float c = cost(v);
            if (c < min)
            {
                min = c;
                min_conf = v;
            }
        }
        if (min_conf.HasValue)
            return optimizePoint(min_conf.Value, cost, out conf_min, smallStep);
        if (!smallStep)
            return optimizePoint(conf, cost, out conf_min, true);
        conf_min = conf;
        return min;
    }

    float ras_cuts_per_unit = 0;
    float RASofLine(Vector3 init, Vector3 target, int max_depth, int opti_max_depth, out Vector3[] out_path)
    {
        // If the max depth has been reached, or if init/target is not an allowed straight move, we return Infinity.
        if (max_depth < 0)
        {
            out_path = new Vector3[] { init, target };
            return Mathf.Infinity;
        }
        bool clockwise = phy.clockwisePreferedForMove(init, target);
        if (!phy.moveAllowed(init, target, clockwise))
        {
            out_path = new Vector3[] { init, target };
            return Mathf.Infinity;
        }
        // We try a r&s trajectory from init to target. If it is not an allowed path, we split the segment in two parts and compute r&s recursively on it.
        ReedAndShepp.ReedAndShepp.Vector3[] ras_path;
        float l = (float)ras.ComputeCurve(Misc.UnityConfToRSConf(init), Misc.UnityConfToRSConf(target), 0.1, out ras_path);
        out_path = Misc.RSPathToUnityPath(ras_path);
        if (phy.pathAllowed(out_path))
            return l;
        else
        {
            Vector3 diff = CarController.computeDiffVector(init, target, clockwise);
            int nb_cuts = Mathf.CeilToInt(CarController.magnitudeOfDiffVector(diff) * ras_cuts_per_unit);
            if (nb_cuts < 1) nb_cuts = 1;
            Vector3[] path = new Vector3[nb_cuts + 2];
            path[0] = init; path[nb_cuts + 1] = target;
            for (int i = 1; i < nb_cuts + 1; i++)
                path[i] = init + i * CarController.computeDiffVector(init, target, clockwise) / (nb_cuts + 1);
            return ComputeOptimizedRAS(path, max_depth - 1, opti_max_depth - 1, out out_path);
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
          - If it connects two connected components together, we keep it
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
            Debug.DrawLine(CarController.spatialCoordOfConfiguration(pts[0]), CarController.spatialCoordOfConfiguration(pts[1]), Color.red, 5f);
            components.UnionValues(pts[0], pts[1]);
        }

        int i = 0;
        int cons_rejections = 0;
        Dictionary<Vector3, bool> tmp_dico = new Dictionary<Vector3, bool>();
        while (components.Find(pts[0]).value != components.Find(pts[1]).value || i < minPointsMonteCarlo)
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
                    tmp_dico[v] = linked;
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
                        dico[new Link(v, pt)] = linked ? distanceBetweenConf(v, pt) : Mathf.Infinity;
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
        /* Not show collision
        if (phy.currentlyInCollision())
            controller.changeColor(Color.red);
        else
            controller.changeColor(Color.green);
        */

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
