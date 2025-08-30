using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 이펙트가 타겟에 붙어있을 때 타겟 파괴를 감지하고 풀로 반환하는 컴포넌트
    /// </summary>
    public class EffectTargetTracker : MonoBehaviour
    {
        private Transform trackedTarget;
        private bool isTrackingTarget = false;
        
        /// <summary>
        /// 추적할 타겟 설정
        /// </summary>
        public void SetTarget(Transform target)
        {
            trackedTarget = target;
            isTrackingTarget = target != null;
        }
        
        /// <summary>
        /// 타겟 추적 해제
        /// </summary>
        public void ClearTarget()
        {
            trackedTarget = null;
            isTrackingTarget = false;
        }
        
        private void Update()
        {
            // 타겟이 설정되어 있고 파괴되었는지 확인
            if (isTrackingTarget && (trackedTarget == null || !trackedTarget.gameObject.activeInHierarchy))
            {
                // 타겟이 파괴되었으므로 풀로 반환
                ReturnToPool();
            }
        }
        
        /// <summary>
        /// 풀로 반환
        /// </summary>
        private void ReturnToPool()
        {
            // 추적 중지
            ClearTarget();
            
            // 풀로 반환
            if (EffectObjectPool.Instance != null)
            {
                EffectObjectPool.Instance.ReturnObject(gameObject);
            }
            else
            {
                // 풀이 없으면 오브젝트 파괴
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            // 컴포넌트가 파괴될 때 추적 정리
            ClearTarget();
        }
    }
}