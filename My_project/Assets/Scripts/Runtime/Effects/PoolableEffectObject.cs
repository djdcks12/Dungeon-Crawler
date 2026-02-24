using UnityEngine;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 풀링 가능한 이펙트 오브젝트 컴포넌트
    /// </summary>
    public class PoolableEffectObject : MonoBehaviour
    {
        public System.Action<GameObject> OnReturnToPool;
        
        private Coroutine returnCoroutine;
        
        /// <summary>
        /// 지정된 시간 후 풀로 반환
        /// </summary>
        public void StartReturnTimer(float delay)
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }
            
            returnCoroutine = StartCoroutine(ReturnAfterDelay(delay));
        }
        
        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnReturnToPool?.Invoke(gameObject);
            returnCoroutine = null;
        }
        
        /// <summary>
        /// 즉시 풀로 반환
        /// </summary>
        public void ReturnToPoolImmediate()
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            OnReturnToPool?.Invoke(gameObject);
        }
        
        private void OnDisable()
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}