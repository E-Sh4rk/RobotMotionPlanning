using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarAI : MonoBehaviour {

    public int maxPointsMonteCarlo = 2000;
    public int minPointsMonteCarlo = 500;
    public int maxConsecutiveRejections = 10;
    public int rasMaxDepth = 7;
    public float rasMinStreamingCutsLength = 1;
    public int rasApproxDepth = 0;

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
        return ComputeOptimizedRAS(path, rasMaxDepth, rasMaxDepth, out save_targets, Mathf.Infinity);
    }

    const int nb_cuts_streaming = 1;
    float ComputeOptimizedRAS(Vector3[] p, int max_depth, int opti_max_depth, out Vector3[] opt_path, float max_len)
    {
        opt_path = null;

        Dictionary<Vector3, Junction> middles;
        Vector3[] path = ComputeOptimizedRAS_aux(p, max_depth, opti_max_depth, out middles, max_len);

        /*Vector3[] rev_p = new Vector3[p.Length];
        for (int i = 0; i < p.Length; i++)
            rev_p[rev_p.Length - 1 - i] = p[i];
        Dictionary<Vector3, Junction> rev_middles;
        Vector3[] rev_path = ComputeOptimizedRAS_aux(rev_p, max_depth, opti_max_depth, out rev_middles, max_len);*/
        // TODO : Domain restricted because it was too intensive. Investigate.
        List<Vector3> rev_p_lst = new List<Vector3>();
        float len = 0;
        for (int i = p.Length - 1; i >= 0; i--)
        {
            rev_p_lst.Add(p[i]);
            if (middles.ContainsKey(p[i]))
            {
                len = middles[p[i]].len;
                break;
            }
        }
        Vector3[] rev_p = rev_p_lst.ToArray();
        Dictionary<Vector3, Junction> rev_middles;
        Vector3[] rev_path = ComputeOptimizedRAS_aux(rev_p, max_depth, opti_max_depth, out rev_middles, max_len-len);

        // Junction
        Vector3[] tmp_val;
        float min_len = Mathf.Infinity;
        foreach (Vector3 pt in middles.Keys)
        {
            if (rev_middles.ContainsKey(pt))
            {
                Junction j1 = middles[pt];
                Junction j2 = rev_middles[pt];
                float total_len = j1.len + j2.len;
                if (total_len >= max_len)
                    continue;

                Vector3[] path1 = new Vector3[j1.path_count];
                System.Array.Copy(path, path1, path1.Length);
                Vector3[] path2 = new Vector3[j2.path_count];
                for (int i = 0; i < path2.Length; i++)
                    path2[path2.Length-1-i] = rev_path[i];

                total_len += OptimizedJunction(path1[path1.Length-1], pt, path2[0], max_depth, opti_max_depth, out tmp_val, Mathf.Min(max_len,min_len) - total_len);
                if (total_len >= max_len)
                    continue;

                if (total_len < min_len)
                {
                    min_len = total_len;
                    opt_path = new Vector3[path1.Length + tmp_val.Length + path2.Length - 2];
                    System.Array.Copy(path1, opt_path, path1.Length);
                    System.Array.Copy(tmp_val, 0, opt_path, path1.Length-1, tmp_val.Length);
                    System.Array.Copy(path2, 0, opt_path, path1.Length + tmp_val.Length - 2, path2.Length);
                }
            }
        }
        return min_len;
    }

    struct Junction
    {
        public Junction(int path_count, float len) { this.path_count = path_count; this.len = len; }
        public int path_count;
        public float len;
    }

    Vector3[] ComputeOptimizedRAS_aux(Vector3[] p, int max_depth, int opti_max_depth, out Dictionary<Vector3, Junction> middles, float max_len)
    {
        Vector3[] tmp_val;
        float tmp_len;

        float len = 0;
        Vector3[] p2 = (Vector3[])p.Clone();
        List<Vector3> path = new List<Vector3>();
        path.Add(p2[0]);

        middles = new Dictionary<Vector3, Junction>();
        middles.Add(p2[0], new Junction(path.Count, 0));
        if (p2.Length > 1)
            middles.Add(p2[1], new Junction(path.Count, 0));

        for (int i = 1; i < p2.Length - 1; i++)
        {
            tmp_len = OptimizedRASofLine(p2[i - 1], p2[i], p2[i + 1], max_depth, opti_max_depth, out tmp_val, max_len - len);
            if (tmp_len >= Mathf.Infinity)
            {
                Vector3[] subpath = CutPath(new Vector3[] { p2[i - 1], p2[i], p2[i + 1] }, nb_cuts_streaming, rasMinStreamingCutsLength, null);
                if (subpath.Length <= 3)
                    break;
                else
                {
                    Vector3[] new_p2 = new Vector3[p2.Length + subpath.Length - 3];
                    for (int j = 0; j < i - 1; j++)
                        new_p2[j] = p2[j];
                    for (int j = 0; j < subpath.Length; j++)
                        new_p2[j + i - 1] = subpath[j];
                    for (int j = i + 2; j < p2.Length; j++)
                        new_p2[j + subpath.Length - 3] = p2[j];
                    p2 = new_p2;
                    i--;
                    continue;
                }
            }
            len += tmp_len;
            for (int j = 1; j < tmp_val.Length; j++)
                path.Add(tmp_val[j]);
            p2[i] = tmp_val[tmp_val.Length - 1];
            try { middles.Add(p2[i + 1], new Junction(path.Count, len)); } catch { }
        }

        return path.ToArray();
    }

    // Like OptimizedRASofLine but return a path from p1 to p3.
    float OptimizedJunction(Vector3 p1, Vector3 p2, Vector3 p3, int max_depth, int opti_max_depth, out Vector3[] output, float max_len)
    {
        output = null;
        float len = 0;
        Vector3[] tmp_out1 = new Vector3[] { p1 };
        Vector3[] tmp_out2;
        if (p1 != p2 && p2 != p3)
        {
            len += OptimizedRASofLine(p1, p2, p3, max_depth, opti_max_depth, out tmp_out1, max_len);
            if (len >= Mathf.Infinity)
                return Mathf.Infinity;
        }
        
        len += RASofLine(tmp_out1[tmp_out1.Length-1], p3, max_depth, opti_max_depth>0?opti_max_depth-1:opti_max_depth, out tmp_out2, max_len-len);
        if (len >= Mathf.Infinity)
            return Mathf.Infinity;
        output = new Vector3[tmp_out1.Length + tmp_out2.Length-1];
        System.Array.Copy(tmp_out1, output, tmp_out1.Length);
        System.Array.Copy(tmp_out2, 0, output, tmp_out1.Length-1, tmp_out2.Length);
        return len;
    }

    // Give a path from p1 to an other point near p2 that is optimized in order to go to p3.
    // If going from this intermediate point to p3 is impossible, the len returned will always be infinity even if the len from p1 to the intermediate point is finite.
    float OptimizedRASofLine(Vector3 p1, Vector3 p2, Vector3 p3, int max_depth, int opti_max_depth, out Vector3[] output, float max_len)
    {
        Vector3 tmp_conf;
        float len = 0;
        if (opti_max_depth <= 0)
        {
            // No point optimization
            len = RASofLine(p1, p2, max_depth, opti_max_depth, out output, max_len);
        }
        else
        {
            // Point optimization
            CostFunc cost = (Vector3 v, float max_cost) =>
            {
                Vector3[] devnull;
                float tmp_len = RASofLine(p1, v, max_depth, Mathf.Min(rasApproxDepth, opti_max_depth-1), out devnull, Mathf.Min(max_len, max_cost));
                if (tmp_len < Mathf.Infinity)
                    tmp_len += RASofLine(v, p3, max_depth, Mathf.Min(rasApproxDepth, opti_max_depth-1), out devnull, max_cost-tmp_len);
                return tmp_len;
            };
            len = optimizePoint(p2, cost, out tmp_conf);
            output = null;
            if (len < Mathf.Infinity) // The len that interest us is only the len from p1 to p2
                len = RASofLine(p1, tmp_conf, max_depth, opti_max_depth-1, out output, max_len);
        }
        return len;
    }

    delegate float CostFunc(Vector3 conf, float max_cost);
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
        float min = cost(conf, Mathf.Infinity);
        Vector3? min_conf = null;
        foreach (Vector3 v in all_pos)
        {
            float c = cost(v, min);
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

    const int nb_cuts_recursive = 1;
    float RASofLine(Vector3 init, Vector3 target, int max_depth, int opti_max_depth, out Vector3[] out_path, float max_len)
    {
        out_path = null;
        bool clockwise = phy.clockwisePreferedForMove(init, target);
        if (!phy.moveAllowed(init, target, clockwise))
            return Mathf.Infinity;
        // We try a r&s trajectory from init to target. If it is not an allowed path, we split the segment in two parts and compute r&s recursively on it.
        ReedAndShepp.ReedAndShepp.Vector3[] ras_path;
        float l = (float)ras.ComputeCurve(Misc.UnityConfToRSConf(init), Misc.UnityConfToRSConf(target), 0.1, out ras_path);
        if (l >= max_len)
            return Mathf.Infinity;
        Vector3[] tmp_path = Misc.RSPathToUnityPath(ras_path);
        if (phy.pathAllowed(tmp_path))
        {
            if (tmp_path.Length >= 2)
                out_path = tmp_path;
            else
                out_path = new Vector3[] { init, target };
            return l;
        }
        else if (max_depth > 0)
        {
            Vector3[] path = CutPath(new Vector3[] { init, target }, nb_cuts_recursive, 0, new bool[] { clockwise });
            return ComputeOptimizedRAS(path, max_depth - 1, opti_max_depth, out out_path, max_len);
        }
        else
            return Mathf.Infinity;
    }

    Vector3[] CutPath(Vector3[] p, int nb_cuts, float min_cut_length, bool[] clockwise = null)
    {
        if (nb_cuts < 1) return null;

        List<Vector3> new_p = new List<Vector3>();
        new_p.Add(p[0]);
        for (int i = 0; i < p.Length-1; i++)
        {
            bool cw = clockwise != null ? clockwise[i] : phy.clockwisePreferedForMove(p[i], p[i+1]);
            Vector3 diff = CarController.computeDiffVector(p[i], p[i + 1], cw);
            if (CarController.magnitudeOfDiffVector(diff) / (nb_cuts + 1) >= min_cut_length)
            {
                for (int j = 1; j <= nb_cuts; j++)
                {
                    Vector3 pt = p[i] + j * diff / (nb_cuts + 1);
                    pt.z = CarController.normalizeAngle(pt.z);
                    new_p.Add(pt);
                }
            }
            new_p.Add(p[i + 1]);
        }
        return new_p.ToArray();
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
