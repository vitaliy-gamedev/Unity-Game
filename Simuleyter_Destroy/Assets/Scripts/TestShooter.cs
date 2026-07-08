using UnityEngine;

public class TestShooter : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _force = 30f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            GameObject bullet = Instantiate(_bulletPrefab, cam.transform.position, cam.transform.rotation);

            if (bullet.TryGetComponent(out Rigidbody rb))
                rb.AddForce(cam.transform.forward * _force, ForceMode.Impulse);
        }
    }
}
