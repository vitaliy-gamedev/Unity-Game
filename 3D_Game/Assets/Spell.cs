using UnityEngine;
using System.Collections.Generic;

public class MagicSkills : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _fireballPrefab;
    [SerializeField] private GameObject _spikePrefab;
    [SerializeField] private Transform _spawnPoint;

    [Header("Fireball")]
    [SerializeField] private float _fireballForce = 20f;
    [SerializeField] private float _fireballMaxDistance = 20f;

    [Header("Hook")]
    [SerializeField] private float _hookDistance = 20f;
    [SerializeField] private float _hookMinDistance = 5f;
    [SerializeField] private float _hookPullSpeed = 10f;
    [SerializeField] private float _hookArrivalDistance = 1.5f; // коли вважати що піймав

    [Header("Spikes")]
    [SerializeField] private float _spikeLifetime = 2f;
    [SerializeField] private int _maxSpikes = 5;

    private List<GameObject> _activeSpikes = new List<GameObject>();
    private Coroutine _hookCoroutine;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) CastFireball();
        if (Input.GetKeyDown(KeyCode.C)) SpawnSpike();
        if (Input.GetKeyDown(KeyCode.B)) Hook();
    }

    private void CastFireball()
    {
        GameObject ball = Instantiate(_fireballPrefab, _spawnPoint.position, _spawnPoint.rotation);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = _spawnPoint.forward * _fireballForce;
        Destroy(ball, _fireballMaxDistance / _fireballForce);
    }

    private void SpawnSpike()
    {
        if (_activeSpikes.Count >= _maxSpikes)
        {
            Destroy(_activeSpikes[0]);
            _activeSpikes.RemoveAt(0);
        }

        Vector3 spawnPos = transform.position + transform.forward * 3f;
        GameObject spike = Instantiate(_spikePrefab, spawnPos, Quaternion.identity);
        _activeSpikes.Add(spike);
        Destroy(spike, _spikeLifetime);
        StartCoroutine(RemoveSpikeLater(spike));
    }

    private System.Collections.IEnumerator RemoveSpikeLater(GameObject spike)
    {
        yield return new WaitForSeconds(_spikeLifetime);
        if (_activeSpikes.Contains(spike)) _activeSpikes.Remove(spike);
    }

    private void Hook()
    {
        // Якщо вже тягнемо — зупинити
        if (_hookCoroutine != null)
        {
            StopCoroutine(_hookCoroutine);
            _hookCoroutine = null;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(_spawnPoint.position, _spawnPoint.forward, out hit, _hookDistance))
        {
            if (hit.collider.CompareTag("Hookable"))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance >= _hookMinDistance)
                {
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        _hookCoroutine = StartCoroutine(PullObject(rb));
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator PullObject(Rigidbody rb)
    {
        while (rb != null)
        {
            float distance = Vector3.Distance(transform.position, rb.position);

            if (distance <= _hookArrivalDistance)
            {
                rb.linearVelocity = Vector3.zero;
                _hookCoroutine = null;
                yield break;
            }

            Vector3 direction = (transform.position - rb.position).normalized;
            rb.linearVelocity = direction * _hookPullSpeed;

            yield return null;
        }

        _hookCoroutine = null;
    }
}