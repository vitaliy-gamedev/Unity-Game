using UnityEngine;

/// <summary>
/// Builds a drone 3D model from Unity primitives and attaches all required components.
/// Drop this on an empty GameObject at the scene root.
/// Set droneType before Play.
/// </summary>
public class DroneBuilder : MonoBehaviour
{
    public enum DroneVariant { Scout, Bomber }
    public DroneVariant droneType = DroneVariant.Scout;

    [Header("Override spawn position")]
    public Vector3 spawnPosition = new Vector3(0, 20f, 0);

    private GameObject _droneRoot;

    void Awake()
    {
        _droneRoot = BuildDrone();
        _droneRoot.transform.position = spawnPosition;
    }

    GameObject BuildDrone()
    {
        var root = new GameObject(droneType == DroneVariant.Scout ? "ScoutDrone" : "BomberDrone");

        if (droneType == DroneVariant.Scout)
            BuildMavicStyle(root);
        else
            BuildVampirStyle(root);

        // Camera
        var camGO = new GameObject("DroneCamera");
        camGO.transform.SetParent(root.transform, false);
        camGO.transform.localPosition = new Vector3(0, 0.3f, 0.6f);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView   = 70f;
        cam.nearClipPlane = 0.05f;
        camGO.AddComponent<AudioListener>();

        // Physics
        var rb       = root.AddComponent<Rigidbody>();
        rb.mass      = 1f;
        rb.useGravity = false;

        // Drop point for bomber
        if (droneType == DroneVariant.Bomber)
        {
            var dp = new GameObject("DropPoint");
            dp.transform.SetParent(root.transform, false);
            dp.transform.localPosition = new Vector3(0, -0.3f, 0);
        }

        // Wire controller
        AttachController(root, cam);

        return root;
    }

    // ── Mavic-style Scout ────────────────────────────────────────
    void BuildMavicStyle(GameObject root)
    {
        Color bodyColor = new Color(0.15f, 0.15f, 0.15f);
        Color armColor  = new Color(0.2f, 0.2f, 0.2f);
        Color propColor = new Color(0.9f, 0.9f, 0.9f);

        // Body
        var body = MakePart("Body", root, Vector3.zero,
            new Vector3(0.35f, 0.1f, 0.5f), PrimitiveType.Cube, bodyColor);

        // Camera gimbal
        var gimbal = MakePart("Gimbal", root, new Vector3(0, -0.05f, 0.26f),
            new Vector3(0.12f, 0.1f, 0.08f), PrimitiveType.Cube, new Color(0.3f, 0.3f, 0.3f));
        var lens = MakePart("Lens", root, new Vector3(0, -0.05f, 0.32f),
            Vector3.one * 0.07f, PrimitiveType.Sphere, new Color(0.05f, 0.1f, 0.4f));

        // Four arms + propellers
        float[,] arms = { {  0.28f, 0,  0.28f }, { -0.28f, 0,  0.28f },
                          {  0.28f, 0, -0.28f }, { -0.28f, 0, -0.28f } };
        for (int i = 0; i < 4; i++)
        {
            float ax = arms[i, 0], ay = arms[i, 1], az = arms[i, 2];
            MakePart($"Arm{i}", root, new Vector3(ax * 0.5f, ay, az * 0.5f),
                new Vector3(0.06f, 0.04f, 0.06f + Mathf.Abs(az) * 0.3f), PrimitiveType.Cube, armColor);

            var prop = MakePart($"Prop{i}", root, new Vector3(ax, 0.04f, az),
                new Vector3(0.28f, 0.015f, 0.06f), PrimitiveType.Cube, propColor);
            prop.AddComponent<PropellerSpin>().speedMultiplier = (i % 2 == 0 ? 1f : -1f);
        }
    }

    // ── Vampir-style Bomber ──────────────────────────────────────
    void BuildVampirStyle(GameObject root)
    {
        Color bodyColor = new Color(0.1f, 0.12f, 0.1f);
        Color wingColor = new Color(0.15f, 0.15f, 0.12f);
        Color propColor = new Color(0.7f, 0.7f, 0.7f);

        // Main body (elongated)
        MakePart("Body", root, Vector3.zero,
            new Vector3(0.25f, 0.12f, 0.8f), PrimitiveType.Cube, bodyColor);

        // Nose cone
        var nose = MakePart("Nose", root, new Vector3(0, 0, 0.5f),
            Vector3.one * 0.18f, PrimitiveType.Sphere, bodyColor);

        // Wings (wide)
        MakePart("WingL", root, new Vector3(-0.55f, 0, 0),
            new Vector3(0.7f, 0.05f, 0.35f), PrimitiveType.Cube, wingColor);
        MakePart("WingR", root, new Vector3( 0.55f, 0, 0),
            new Vector3(0.7f, 0.05f, 0.35f), PrimitiveType.Cube, wingColor);

        // Tail fins
        MakePart("TailH", root, new Vector3(0, 0, -0.44f),
            new Vector3(0.4f, 0.04f, 0.22f), PrimitiveType.Cube, wingColor);
        MakePart("TailV", root, new Vector3(0, 0.15f, -0.44f),
            new Vector3(0.04f, 0.3f, 0.22f), PrimitiveType.Cube, wingColor);

        // Pusher prop (rear)
        var prop = MakePart("Prop", root, new Vector3(0, 0, -0.6f),
            new Vector3(0.45f, 0.02f, 0.06f), PrimitiveType.Cube, propColor);
        prop.AddComponent<PropellerSpin>().speedMultiplier = 1f;

        // Bomb hardpoints
        MakePart("BombL", root, new Vector3(-0.3f, -0.1f, 0),
            new Vector3(0.08f, 0.08f, 0.25f), PrimitiveType.Cylinder, new Color(0.1f, 0.1f, 0.1f));
        MakePart("BombR", root, new Vector3( 0.3f, -0.1f, 0),
            new Vector3(0.08f, 0.08f, 0.25f), PrimitiveType.Cylinder, new Color(0.1f, 0.1f, 0.1f));
    }

    // ── Helpers ───────────────────────────────────────────────────
    GameObject MakePart(string name, GameObject parent, Vector3 localPos,
                        Vector3 scale, PrimitiveType prim, Color color)
    {
        var g = GameObject.CreatePrimitive(prim);
        g.name = name;
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localScale    = scale;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        g.GetComponent<Renderer>().material = mat;
        // Remove colliders from visual parts (root handles physics)
        Destroy(g.GetComponent<Collider>());
        return g;
    }

    void AttachController(GameObject root, Camera cam)
    {
        // Add box collider to root
        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(1.2f, 0.3f, 1.2f);
        col.center = Vector3.zero;

        if (droneType == DroneVariant.Scout)
        {
            var ctrl = root.AddComponent<ScoutDroneController>();
            ctrl.droneCamera = cam;
        }
        else
        {
            var ctrl = root.AddComponent<BomberDroneController>();
            ctrl.droneCamera = cam;
            ctrl.dropPoint   = root.transform.Find("DropPoint");
        }

        // Audio source
        var audio = root.AddComponent<AudioSource>();
        audio.spatialBlend = 0f; // 2D for FPV feel

        if (droneType == DroneVariant.Scout)
            root.GetComponent<ScoutDroneController>().motorAudioSource = audio;
        else
            root.GetComponent<BomberDroneController>().motorAudioSource = audio;
    }
}

/// <summary>Spins a propeller transform continuously.</summary>
public class PropellerSpin : MonoBehaviour
{
    public float speedMultiplier = 1f;
    public float baseRPM = 3000f;

    void Update() => transform.Rotate(0, baseRPM * speedMultiplier * Time.deltaTime, 0);
}
