using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 카메라가 타겟(플레이어)을 부드럽게 따라가는 시스템
    /// 로컬 플레이어 스폰 시 PlayerController에서 자동 설정
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        private Transform target;

        public void SetTarget(Transform t)
        {
            target = t;
            if (target != null)
            {
                // 즉시 타겟 위치로 이동 (스폰 시 점프 방지)
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }
    }
}
