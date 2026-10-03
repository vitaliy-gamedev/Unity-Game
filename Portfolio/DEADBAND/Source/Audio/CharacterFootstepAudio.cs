using Deadband.Combat;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class CharacterFootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private bool playerControlled;
        [SerializeField] private bool heavyCharacter;

        private Health health;
        private PlayerUnitController playerController;
        private NavMeshAgent navMeshAgent;
        private Vector3 previousPosition;
        private float travelledDistance;
        private bool initialized;

        public bool IsConfigured => audioSource != null && footstepClip != null;
        public AudioSource Source => audioSource;
        public int PlayCount { get; private set; }

        public void Configure(AudioSource source, AudioClip clip, bool isPlayer, bool isHeavy)
        {
            audioSource = source;
            footstepClip = clip;
            playerControlled = isPlayer;
            heavyCharacter = isHeavy;
            ConfigureAudioSource();
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            playerController = GetComponent<PlayerUnitController>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            ConfigureAudioSource();
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            travelledDistance = 0f;
            initialized = true;
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;
            if (!initialized)
            {
                previousPosition = currentPosition;
                initialized = true;
                return;
            }

            Vector3 displacement = currentPosition - previousPosition;
            previousPosition = currentPosition;
            displacement.y = 0f;

            float distance = displacement.magnitude;
            if (!IsConfigured || health != null && !health.IsAlive || distance > 4f ||
                Time.deltaTime <= 0f || !IsGrounded())
            {
                if (distance > 4f) travelledDistance = 0f;
                return;
            }

            float speed = distance / Time.deltaTime;
            if (speed < 0.35f)
            {
                return;
            }

            travelledDistance += distance;
            float stride = Mathf.Lerp(1.75f, 1.12f, Mathf.InverseLerp(1.1f, 7.5f, speed));
            if (heavyCharacter) stride *= 1.18f;
            if (travelledDistance < stride)
            {
                return;
            }

            travelledDistance %= stride;
            PlayStep(speed);
        }

        public void PlayStepForDiagnostics()
        {
            PlayStep(4f);
        }

        private bool IsGrounded()
        {
            if (playerController != null)
            {
                return playerController.IsGrounded;
            }
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                return navMeshAgent.isOnNavMesh;
            }
            return Physics.Raycast(
                transform.position + Vector3.up * 0.25f,
                Vector3.down,
                0.75f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void PlayStep(float speed)
        {
            if (!IsConfigured)
            {
                return;
            }

            float speedFactor = Mathf.InverseLerp(0.8f, 7.5f, speed);
            float basePitch = heavyCharacter ? 0.74f : 1f;
            audioSource.pitch = basePitch * Random.Range(0.91f, 1.08f);
            float volume = Mathf.Lerp(0.2f, playerControlled ? 0.43f : 0.34f, speedFactor);
            if (heavyCharacter) volume *= 1.42f;
            audioSource.PlayOneShot(footstepClip, volume);
            PlayCount++;
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = playerControlled ? 0.25f : 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.4f;
            audioSource.maxDistance = heavyCharacter ? 30f : 20f;
            audioSource.dopplerLevel = 0f;
        }
    }
}
