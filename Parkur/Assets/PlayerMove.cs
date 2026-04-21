using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Шукаємо камеру в дочірніх об'єктах
        Camera cam = GetComponentInChildren<Camera>();

        if (cam != null)
        {
            cameraTransform = cam.transform;
        }

        // Перевірка на помилки
        if (controller == null) Debug.LogError("!!! НА ГРАВЦЕВІ НЕМАЄ CHARACTER CONTROLLER !!!");
        if (cameraTransform == null) Debug.LogError("!!! КАМЕРА НЕ ЗНАЙДЕНА ВСЕРЕДИНІ ГРАВЦЯ !!!");

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Якщо чогось не вистачає, виходимо, щоб не було NullReferenceException
        if (controller == null || cameraTransform == null) return;

        // Повороти
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // Рух
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }
}