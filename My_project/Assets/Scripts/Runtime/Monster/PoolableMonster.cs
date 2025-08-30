using UnityEngine;
using System;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 풀 가능한 몬스터 오브젝트 컴포넌트
    /// </summary>
    public class PoolableMonster : MonoBehaviour
    {
        public event Action<GameObject> OnReturnToPool;
        
        private float returnTimer = 0f;
        private bool hasReturnTimer = false;
        
        /// <summary>
        /// 일정 시간 후 풀로 반환하는 타이머 시작
        /// </summary>
        public void StartReturnTimer(float delay)
        {
            returnTimer = delay;
            hasReturnTimer = true;
        }
        
        /// <summary>
        /// 반환 타이머 취소
        /// </summary>
        public void CancelReturnTimer()
        {
            hasReturnTimer = false;
            returnTimer = 0f;
        }
        
        /// <summary>
        /// 즉시 풀로 반환
        /// </summary>
        public void ReturnToPool()
        {
            CancelReturnTimer();
            OnReturnToPool?.Invoke(gameObject);
        }
        
        private void Update()
        {
            if (hasReturnTimer)
            {
                returnTimer -= Time.deltaTime;
                if (returnTimer <= 0f)
                {
                    ReturnToPool();
                }
            }
        }
        
        private void OnDestroy()
        {
            CancelReturnTimer();
        }
        
        /// <summary>
        /// 몬스터가 죽었을 때 호출
        /// </summary>
        public void OnMonsterDeath()
        {
            // 죽은 몬스터는 바로 풀로 반환하지 않고 잠깐 대기
            StartReturnTimer(2f); // 2초 후 풀로 반환
        }
    }
}