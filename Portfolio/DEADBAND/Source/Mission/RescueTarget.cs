using System;
using Deadband.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Deadband.Missions
{
    [DisallowMultipleComponent]
    public sealed class RescueTarget : MonoBehaviour
    {
        [SerializeField] private Transform leader;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private Light locatorLight;
        [SerializeField, Min(1f)] private float interactionRange = 3.2f;
        [SerializeField, Min(1f)] private float followDistance = 2.35f;

        private bool objectiveAvailable;
        private float feedbackUntil;

        public event Action Rescued;
        public bool IsRescued { get; private set; }
        public bool IsAlive => health == null || health.IsAlive;
        public Vector3 Position => transform.position;

        public void Configure(Transform followLeader, NavMeshAgent navAgent, Animator targetAnimator, Health targetHealth, Light beacon)
        {
            leader = followLeader;
            agent = navAgent;
            animator = targetAnimator;
            health = targetHealth;
            locatorLight = beacon;
        }

        public void SetObjectiveAvailable(bool available)
        {
            objectiveAvailable = available;
            if (locatorLight != null)
            {
                locatorLight.color = available ? new Color(0.2f, 1f, 0.46f) : new Color(1f, 0.62f, 0.1f);
                locatorLight.intensity = available ? 5f : 2f;
            }
        }

        private void Update()
        {
            if (!IsAlive || leader == null) return;

            if (!IsRescued)
            {
                Keyboard keyboard = Keyboard.current;
                if (objectiveAvailable && Vector3.Distance(transform.position, leader.position) <= interactionRange &&
                    keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    IsRescued = true;
                    feedbackUntil = Time.unscaledTime + 4f;
                    if (locatorLight != null) locatorLight.color = new Color(0.2f, 1f, 0.46f);
                    Rescued?.Invoke();
                }
                SetAnimatorSpeed(0f);
                return;
            }

            float distance = Vector3.Distance(transform.position, leader.position);
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (distance > followDistance)
                {
                    agent.isStopped = false;
                    agent.SetDestination(leader.position - leader.forward * 1.9f);
                }
                else
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                SetAnimatorSpeed(agent.velocity.magnitude);
            }
        }

        private void SetAnimatorSpeed(float speed)
        {
            if (animator == null) return;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("MotionSpeed", speed > 0.05f ? 1f : 0f);
            animator.SetBool("Grounded", true);
            animator.SetBool("Aiming", false);
        }

        private void OnGUI()
        {
            if (!IsAlive || leader == null) return;
            float distance = Vector3.Distance(transform.position, leader.position);
            if (!IsRescued && objectiveAvailable && distance <= interactionRange)
            {
                DrawPrompt("[ E ] HELP THE MISSING SCOUT");
            }
            else if (IsRescued && Time.unscaledTime < feedbackUntil)
            {
                DrawPrompt("SCOUT RESCUED  //  ESCORT HIM TO EXTRACTION");
            }
        }

        private static void DrawPrompt(string message)
        {
            GUIStyle style = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 1f, 0.68f) }
            };
            GUI.Label(new Rect(Screen.width * 0.5f - 290f, Screen.height * 0.7f, 580f, 42f), message, style);
        }
    }
}
