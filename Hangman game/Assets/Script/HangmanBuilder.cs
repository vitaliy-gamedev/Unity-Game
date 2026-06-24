using UnityEngine;


public class HangmanBuilder : MonoBehaviour
{
    public HangmanGame gameManager; 
    public Material baseMaterial;
    public Material bodyMaterial;

    [ContextMenu("Build Hangman Structure")]
    public void BuildStructure()
    {
        GameObject root = new GameObject("HangmanStructure");
        root.transform.position = Vector3.zero;

        // --- Нерухомі частини шибениці (завжди видимі) ---
        GameObject baseBlock = CreatePart("Base", PrimitiveType.Cube, root.transform,
            new Vector3(0, -2f, 0), new Vector3(3f, 0.3f, 1.5f), baseMaterial);

        GameObject pole = CreatePart("Pole", PrimitiveType.Cube, root.transform,
            new Vector3(-1f, 0.5f, 0), new Vector3(0.3f, 5f, 0.3f), baseMaterial);

        GameObject beam = CreatePart("Beam", PrimitiveType.Cube, root.transform,
            new Vector3(0f, 2.9f, 0), new Vector3(2f, 0.3f, 0.3f), baseMaterial);

        // --- Частини, що з'являються по черзі при помилках (індекси 0-8) ---
        GameObject rope = CreatePart("Rope", PrimitiveType.Cylinder, root.transform,
            new Vector3(0.8f, 2.3f, 0), new Vector3(0.08f, 0.6f, 0.08f), baseMaterial);

        GameObject head = CreatePart("Head", PrimitiveType.Sphere, root.transform,
            new Vector3(0.8f, 1.5f, 0), new Vector3(0.6f, 0.6f, 0.6f), bodyMaterial);

        GameObject body = CreatePart("Body", PrimitiveType.Capsule, root.transform,
            new Vector3(0.8f, 0.6f, 0), new Vector3(0.4f, 0.7f, 0.4f), bodyMaterial);

        GameObject arm1 = CreatePart("Arm_Left", PrimitiveType.Cylinder, root.transform,
            new Vector3(0.4f, 0.9f, 0), new Vector3(0.1f, 0.5f, 0.1f), bodyMaterial);
        arm1.transform.eulerAngles = new Vector3(0, 0, 45);

        GameObject arm2 = CreatePart("Arm_Right", PrimitiveType.Cylinder, root.transform,
            new Vector3(1.2f, 0.9f, 0), new Vector3(0.1f, 0.5f, 0.1f), bodyMaterial);
        arm2.transform.eulerAngles = new Vector3(0, 0, -45);

        GameObject leg1 = CreatePart("Leg_Left", PrimitiveType.Cylinder, root.transform,
            new Vector3(0.6f, -0.2f, 0), new Vector3(0.1f, 0.5f, 0.1f), bodyMaterial);
        leg1.transform.eulerAngles = new Vector3(0, 0, 20);

        GameObject leg2 = CreatePart("Leg_Right", PrimitiveType.Cylinder, root.transform,
            new Vector3(1.0f, -0.2f, 0), new Vector3(0.1f, 0.5f, 0.1f), bodyMaterial);
        leg2.transform.eulerAngles = new Vector3(0, 0, -20);

        // Заповнити масив у HangmanGame, якщо посилання задане
        if (gameManager != null)
        {
            gameManager.hangmanParts = new GameObject[]
            {
                rope, head, body, arm1, arm2, leg1, leg2
            };
        }

        Debug.Log("Шибеницю побудовано! Не забудь призначити hangmanParts вручну, якщо gameManager не задано.");
    }

    GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;

        if (mat != null)
        {
            obj.GetComponent<Renderer>().material = mat;
        }

        return obj;
    }
}