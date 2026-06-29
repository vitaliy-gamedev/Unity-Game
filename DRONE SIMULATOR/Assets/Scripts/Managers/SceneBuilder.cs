using UnityEngine;

/// <summary>
/// Procedurally builds the mission scene on Start.
/// Attach to an empty "SceneBuilder" GameObject in both Scout and Bomber scenes.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [Header("Terrain")]
    public int   terrainSize  = 200;
    public float terrainScale = 0.05f;
    public Color groundColor  = new Color(0.25f, 0.35f, 0.15f);

    [Header("Environment")]
    public int treesCount     = 60;
    public int buildingsCount = 20;

    [Header("Targets (Scout only)")]
    public bool spawnTargets  = true;
    public int  infantryCount = 5;
    public int  vehicleCount  = 3;
    public int  bunkerCount   = 2;
    public LayerMask groundLayer;

    void Start()
    {
        BuildGround();
        BuildTrees();
        BuildBuildings();
        if (spawnTargets) SpawnTargets();
        SetupLighting();
        PlaceDroneSpawn();
    }

    // ── Ground Plane ─────────────────────────────────────────────
    void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f);
        var mat = new Material(Shader.Find("Standard"));
        mat.color = groundColor;
        ground.GetComponent<Renderer>().material = mat;
        ground.layer = LayerMask.NameToLayer("Default");
    }

    // ── Trees ─────────────────────────────────────────────────────
    void BuildTrees()
    {
        var treeParent = new GameObject("Trees").transform;
        for (int i = 0; i < treesCount; i++)
        {
            Vector3 pos = RandomPos(5f);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.parent        = treeParent;
            trunk.transform.position      = pos + Vector3.up * 1.5f;
            trunk.transform.localScale    = new Vector3(0.3f, 1.5f, 0.3f);
            SetColor(trunk, new Color(0.4f, 0.25f, 0.1f));

            // Canopy
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.parent       = treeParent;
            float h = Random.Range(3f, 6f);
            canopy.transform.position     = pos + Vector3.up * (h);
            canopy.transform.localScale   = new Vector3(Random.Range(2f, 4f), h * 0.6f, Random.Range(2f, 4f));
            SetColor(canopy, new Color(0.15f + Random.Range(0f, 0.1f), 0.35f + Random.Range(0f, 0.15f), 0.1f));
        }
    }

    // ── Buildings ────────────────────────────────────────────────
    void BuildBuildings()
    {
        var bldParent = new GameObject("Buildings").transform;
        for (int i = 0; i < buildingsCount; i++)
        {
            Vector3 pos = RandomPos(10f);
            float   w   = Random.Range(4f, 10f);
            float   h   = Random.Range(3f, 12f);
            float   d   = Random.Range(4f, 10f);

            var bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bld.transform.parent     = bldParent;
            bld.transform.position   = pos + Vector3.up * (h * 0.5f);
            bld.transform.localScale = new Vector3(w, h, d);
            bld.transform.rotation   = Quaternion.Euler(0, Random.Range(0f, 90f), 0);
            SetColor(bld, new Color(Random.Range(0.5f, 0.7f), Random.Range(0.5f, 0.65f), Random.Range(0.5f, 0.6f)));

            // Roof
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.parent     = bldParent;
            roof.transform.position   = pos + Vector3.up * h;
            roof.transform.localScale = new Vector3(w + 0.5f, 0.3f, d + 0.5f);
            roof.transform.rotation   = bld.transform.rotation;
            SetColor(roof, new Color(0.4f, 0.2f, 0.15f));
        }
    }

    // ── Targets ──────────────────────────────────────────────────
    void SpawnTargets()
    {
        var targetParent = new GameObject("Targets").transform;
        int targetLayer  = LayerMask.NameToLayer("Target");
        if (targetLayer < 0) targetLayer = 0; // fallback

        SpawnGroup(infantryCount, TargetEntity.TargetTypeEnum.Infantry, targetParent, targetLayer, BuildInfantry);
        SpawnGroup(vehicleCount,  TargetEntity.TargetTypeEnum.Vehicle,  targetParent, targetLayer, BuildVehicle);
        SpawnGroup(bunkerCount,   TargetEntity.TargetTypeEnum.Bunker,   targetParent, targetLayer, BuildBunker);
    }

    void SpawnGroup(int count, TargetEntity.TargetTypeEnum type, Transform parent, int layer,
                    System.Func<Vector3, GameObject> builder)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3    pos = RandomPos(20f);
            GameObject obj = builder(pos);
            obj.transform.parent = parent;
            SetLayerRecursive(obj, layer);

            var entity = obj.AddComponent<TargetEntity>();
            entity.type = type;

            if (type == TargetEntity.TargetTypeEnum.Infantry || type == TargetEntity.TargetTypeEnum.Vehicle)
                obj.AddComponent<TargetMovement>();
        }
    }

    // Infantry = capsule (person silhouette)
    GameObject BuildInfantry(Vector3 pos)
    {
        var g = new GameObject("Infantry");
        g.transform.position = pos + Vector3.up * 0.9f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.parent     = g.transform;
        body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
        body.transform.position   = g.transform.position;
        SetColor(body, new Color(0.25f, 0.3f, 0.2f));

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.parent     = g.transform;
        head.transform.position   = g.transform.position + Vector3.up * 1f;
        head.transform.localScale = Vector3.one * 0.35f;
        SetColor(head, new Color(0.8f, 0.65f, 0.5f));

        return g;
    }

    // Vehicle = flattened box with wheels
    GameObject BuildVehicle(Vector3 pos)
    {
        var g = new GameObject("Vehicle");
        g.transform.position = pos + Vector3.up * 0.6f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.parent     = g.transform;
        body.transform.localScale = new Vector3(2.2f, 0.7f, 4f);
        body.transform.position   = g.transform.position;
        SetColor(body, new Color(0.3f, 0.35f, 0.2f));

        // Cab
        var cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cab.transform.parent     = g.transform;
        cab.transform.localScale = new Vector3(1.8f, 0.7f, 2f);
        cab.transform.position   = g.transform.position + new Vector3(0, 0.7f, 0.5f);
        SetColor(cab, new Color(0.28f, 0.33f, 0.18f));

        // Wheels
        for (int i = 0; i < 4; i++)
        {
            float wx = (i % 2 == 0 ? 1 : -1) * 1.2f;
            float wz = (i < 2 ? 1 : -1) * 1.3f;
            var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.transform.parent     = g.transform;
            w.transform.position   = g.transform.position + new Vector3(wx, -0.25f, wz);
            w.transform.rotation   = Quaternion.Euler(0, 0, 90);
            w.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            SetColor(w, Color.black);
        }
        return g;
    }

    // Bunker = low box with sandbag ring
    GameObject BuildBunker(Vector3 pos)
    {
        var g = new GameObject("Bunker");
        g.transform.position = pos;

        var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
        main.transform.parent     = g.transform;
        main.transform.position   = pos + Vector3.up * 0.8f;
        main.transform.localScale = new Vector3(4f, 1.6f, 4f);
        SetColor(main, new Color(0.45f, 0.4f, 0.3f));

        // Sandbags ring
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            var sb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sb.transform.parent   = g.transform;
            sb.transform.position = pos + new Vector3(Mathf.Cos(angle) * 2.3f, 1.6f, Mathf.Sin(angle) * 2.3f);
            sb.transform.localScale = new Vector3(1.1f, 0.5f, 0.5f);
            sb.transform.rotation = Quaternion.Euler(0, i * 45f, 0);
            SetColor(sb, new Color(0.5f, 0.45f, 0.3f));
        }
        return g;
    }

    // ── Lighting ─────────────────────────────────────────────────
    void SetupLighting()
    {
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
        var sun = FindObjectOfType<Light>();
        if (sun != null)
        {
            sun.color     = new Color(1f, 0.95f, 0.8f);
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
        }
        RenderSettings.fogColor   = new Color(0.6f, 0.65f, 0.7f);
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance   = 250f;
    }

    // ── Drone spawn ───────────────────────────────────────────────
    void PlaceDroneSpawn()
    {
        // Drone should already be in scene; just ensure it starts at a good altitude
        var drone = FindObjectOfType<ScoutDroneController>();
        if (drone != null && drone.transform.position == Vector3.zero)
            drone.transform.position = new Vector3(0, 15f, 0);

        var bomber = FindObjectOfType<BomberDroneController>();
        if (bomber != null && bomber.transform.position == Vector3.zero)
            bomber.transform.position = new Vector3(0, 20f, 0);
    }

    // ── Helpers ───────────────────────────────────────────────────
    Vector3 RandomPos(float border)
    {
        float half = terrainSize / 2f - border;
        return new Vector3(Random.Range(-half, half), 0, Random.Range(-half, half));
    }

    void SetColor(GameObject g, Color c)
    {
        var r = g.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = c;
        r.material = mat;
    }

    void SetLayerRecursive(GameObject g, int layer)
    {
        g.layer = layer;
        foreach (Transform child in g.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
