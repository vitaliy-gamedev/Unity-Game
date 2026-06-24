using UnityEngine;


public class CameraOrbit : MonoBehaviour
{
    public Transform target;        
    public float distance = 6f;
    public float rotationSpeed = 100f;
    public float minY = 5f;
    public float maxY = 60f;

    private float currentX = 0f;
    private float currentY = 20f;

    void Start()
    {
        if (target == null)
        {
            GameObject go = GameObject.Find("HangmanStructure");
            if (go != null) target = go.transform;
        }
        UpdateCameraPosition();
    }

    void Update()
    {
        if (Input.GetMouseButton(1)) // права кнопка миші для обертання
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            currentY = Mathf.Clamp(currentY, minY, maxY);
        }

        // Колесо миші - зум
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * 5f, 3f, 15f);

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = target.position + rotation * new Vector3(0, 0, -distance);

        transform.position = position;
        transform.LookAt(target);
    }
}