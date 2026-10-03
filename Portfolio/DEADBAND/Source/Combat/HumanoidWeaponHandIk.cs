using UnityEngine;

namespace Deadband.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class HumanoidWeaponHandIk : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform characterRoot;
        [SerializeField] private GameObject rifleVisual;
        [SerializeField] private GameObject pistolVisual;
        [SerializeField, Range(0f, 1f)] private float rifleLeftHandWeight = 0.66f;
        [SerializeField, Range(0f, 1f)] private float pistolLeftHandWeight = 0f;

        public void Configure(
            Animator targetAnimator,
            Transform targetCharacterRoot,
            GameObject rifle,
            GameObject pistol = null)
        {
            animator = targetAnimator;
            characterRoot = targetCharacterRoot;
            rifleVisual = rifle;
            pistolVisual = pistol;
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || !animator.isHuman || characterRoot == null)
            {
                return;
            }

            bool rifleActive = rifleVisual != null && rifleVisual.activeInHierarchy;
            bool pistolActive = pistolVisual != null && pistolVisual.activeInHierarchy;
            if (!rifleActive && !pistolActive)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                return;
            }

            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(characterRoot.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            float humanScale = Mathf.Max(0.75f, animator.humanScale);
            Vector3 targetPosition;
            float positionWeight;
            if (rifleActive)
            {
                Vector3 rifleDirection = rifleVisual.transform.forward.normalized;
                if (rifleDirection.sqrMagnitude < 0.01f)
                {
                    rifleDirection = forward;
                }
                if (TryGetRendererBounds(rifleVisual, out Bounds rifleBounds))
                {
                    float forwardExtent = Vector3.Dot(
                        rifleBounds.extents,
                        new Vector3(
                            Mathf.Abs(rifleDirection.x),
                            Mathf.Abs(rifleDirection.y),
                            Mathf.Abs(rifleDirection.z)));
                    targetPosition = rifleBounds.center + rifleDirection * (forwardExtent * 0.18f);
                }
                else
                {
                    targetPosition = rightHand.position +
                                     forward * (0.34f * humanScale) +
                                     Vector3.up * (0.015f * humanScale);
                }
                positionWeight = rifleLeftHandWeight;
            }
            else
            {
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                targetPosition = rightHand.position +
                                 forward * (0.105f * humanScale) -
                                 right * (0.075f * humanScale);
                positionWeight = pistolLeftHandWeight;
            }

            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftUpperArm != null && leftLowerArm != null && leftHand != null)
            {
                float armLength = Vector3.Distance(leftUpperArm.position, leftLowerArm.position) +
                                  Vector3.Distance(leftLowerArm.position, leftHand.position);
                Vector3 shoulderToTarget = targetPosition - leftUpperArm.position;
                float maximumReach = armLength * 0.92f;
                if (shoulderToTarget.sqrMagnitude > maximumReach * maximumReach)
                {
                    targetPosition = leftUpperArm.position + shoulderToTarget.normalized * maximumReach;
                }
                targetPosition = Vector3.Lerp(leftHand.position, targetPosition, 0.78f);
            }

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, targetPosition);
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return true;
        }
    }
}
