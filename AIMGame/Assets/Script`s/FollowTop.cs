using UnityEngine;

public class FollowTop : MonoBehaviour
{
    [SerializeField] private Transform _player;

    [SerializeField] private float _height = 40f;

    // FIX #5: прибрано хардкод ротації — нею керує CameraSystem через _topCameraRotation
    // Залишаємо лише слідування за позицією та опціональне слідування по осі Y
    [SerializeField] private bool _followPlayerYRotation = false;

    private void LateUpdate()
    {
        if (_player == null) return;

        transform.position = _player.position + Vector3.up * _height;

        // Якщо потрібно — повертаємо камеру слідом за гравцем по горизонталі.
        // Кут по X (нахил донизу) встановлює CameraSystem, тому тут не чіпаємо його.
        if (_followPlayerYRotation)
        {
            float currentX = transform.rotation.eulerAngles.x;
            transform.rotation = Quaternion.Euler(currentX, _player.eulerAngles.y, 0f);
        }
    }
}