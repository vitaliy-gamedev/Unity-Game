using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Procedurally builds the mission scene on Start.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [Header("Terrain")]
    public int terrainSize = 200;
    public float terrainScale = 0.05f;
    public Color groundColor = new Color(0.25f, 0.35f, 0.15f);

    [Header("Environment")]
    public int treesCount = 60;
    public int buildingsCount = 20;

    [Header("Targets (Scout only)")]
    public bool spawnTargets = true;
    public int infantryCount = 5;
    public int vehicleCount = 3;
    public int bunkerCount = 2;

    void Start()
    {
        BuildGround();
        BuildTrees();
        BuildBuildings();
        if (spawnTargets) SpawnTargets();
        SetupLighting();
        PlaceDroneSpawn();
    }

    void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f);
        SetColor(ground, groundColor);
        ground.isStatic = true;
    }

    void BuildTrees()
    {
        var treeParent = new GameObject("Trees").transform;
        for (int i = 0; i < treesCount; i++)
        {
            Vector3 pos = RandomPos(5f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(treeParent);
            trunk.transform.position = pos + Vector3.up * 1.5f;
            trunk.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
            SetColor(trunk, new Color(0.4f, 0.25f, 0.1f));
            trunk.isStatic = true;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(treeParent);
            float h = Random.Range(3f, 6f);
            canopy.transform.position = pos + Vector3.up * h;
            canopy.transform.localScale = new Vector3(Random.Range(2f, 4f), h * 0.6f, Random.Range(2f, 4f));
            SetColor(canopy, new Color(0.15f + Random.Range(0f, 0.1f), 0.35f + Random.Range(0f, 0.15f), 0.1f));
            canopy.isStatic = true;
        }
    }

    void BuildBuildings()
    {
        var bldParent = new GameObject("Buildings").transform;
        for (int i = 0; i < buildingsCount; i++)
        {
            Vector3 pos = RandomPos(10f);
            float w = Random.Range(4f, 10f), h = Random.Range(3f, 12f), d = Random.Range(4f, 10f);

            var bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bld.transform.SetParent(bldParent);
            bld.transform.position = pos + Vector3.up * (h * 0.5f);
            bld.transform.localScale = new Vector3(w, h, d);
            bld.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 90f), 0);
            SetColor(bld, new Color(Random.Range(0.5f, 0.7f), Random.Range(0.5f, 0.65f), Random.Range(0.5f, 0.6f)));
            bld.isStatic = true;
        }
    }

    void SpawnTargets()
    {
        var targetParent = new GameObject("Targets").transform;
        SpawnGroup(infantryCount, TargetEntity.TargetTypeEnum.Infantry, targetParent, BuildInfantry);
        SpawnGroup(vehicleCount, TargetEntity.TargetTypeEnum.Vehicle, targetParent, BuildVehicle);
        SpawnGroup(bunkerCount, TargetEntity.TargetTypeEnum.Bunker, targetParent, BuildBunker);
    }

    void SpawnGroup(int count, TargetEntity.TargetTypeEnum type, Transform parent, System.Func<Vector3, GameObject> builder)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = builder(RandomPos(20f));
            obj.transform.SetParent(parent);
            var entity = obj.AddComponent<TargetEntity>();
            entity.type = type;
            if (type != TargetEntity.TargetTypeEnum.Bunker) obj.AddComponent<TargetMovement>();
        }
    }

    GameObject BuildInfantry(Vector3 pos)
    {
        var g = new GameObject("Infantry");
        g.transform.position = pos + Vector3.up * 0.9f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(g.transform);
        body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
        SetColor(body, new Color(0.25f, 0.3f, 0.2f));
        return g;
    }

    GameObject BuildVehicle(Vector3 pos)
    {
        var g = new GameObject("Vehicle");
        g.transform.position = pos + Vector3.up * 0.6f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(g.transform);
        body.transform.localScale = new Vector3(2.2f, 0.7f, 4f);
        SetColor(body, new Color(0.3f, 0.35f, 0.2f));
        return g;
    }

    GameObject BuildBunker(Vector3 pos)
    {
        var g = new GameObject("Bunker");
        g.transform.position = pos;
        var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
        main.transform.SetParent(g.transform);
        main.transform.position = pos + Vector3.up * 0.8f;
        main.transform.localScale = new Vector3(4f, 1.6f, 4f);
        SetColor(main, new Color(0.45f, 0.4f, 0.3f));
        return g;
    }

    void SetupLighting()
    {
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 250f;
    }

    void PlaceDroneSpawn()
    {
        var drone = FindObjectOfType<ScoutDroneController>();
        if (drone != null) drone.transform.position = new Vector3(0, 15f, 0);
    }

    Vector3 RandomPos(float border)
    {
        float half = terrainSize / 2f - border;
        return new Vector3(Random.Range(-half, half), 0, Random.Range(-half, half));
    }

    void SetColor(GameObject g, Color c)
    {
        var r = g.GetComponent<Renderer>();
        if (r == null) return;

        // Вибір шейдера: URP якщо доступний, інакше стандартний
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = c;
        r.material = mat;
    }
}