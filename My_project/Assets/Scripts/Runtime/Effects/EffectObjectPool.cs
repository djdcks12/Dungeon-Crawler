using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 이펙트 오브젝트 풀 관리자
    /// </summary>
    public class EffectObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;
        
        private Queue<GameObject> availableObjects = new Queue<GameObject>();
        private List<GameObject> activeObjects = new List<GameObject>();
        
        public static EffectObjectPool Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPoolObject();
            }
        }
        
        private GameObject CreateNewPoolObject()
        {
            var poolObject = new GameObject("PooledEffectObject");
            poolObject.SetActive(false);
            poolObject.transform.SetParent(transform);
            
            // SimpleEffectAnimator와 SpriteRenderer 미리 추가
            poolObject.AddComponent<SpriteRenderer>();
            var animator = poolObject.AddComponent<SimpleEffectAnimator>();
            
            // 풀 반환 이벤트 등록
            var poolable = poolObject.AddComponent<PoolableEffectObject>();
            poolable.OnReturnToPool += ReturnObject;
            
            availableObjects.Enqueue(poolObject);
            poolObject.transform.localScale = new Vector3(5, 5, 1);
            poolObject.GetComponent<SpriteRenderer>().sortingLayerName = "Effects";
            return poolObject;
        }
        
        /// <summary>
        /// 풀에서 오브젝트 가져오기 (타겟에 붙이기 옵션)
        /// </summary>
        public GameObject GetPooledObject(Vector3 position, Quaternion rotation, Transform target = null)
        {
            GameObject pooledObject;
            
            if (availableObjects.Count > 0)
            {
                pooledObject = availableObjects.Dequeue();
            }
            else if (activeObjects.Count < maxPoolSize)
            {
                pooledObject = CreateNewPoolObject();
            }
            else
            {
                // 가장 오래된 활성 오브젝트 재사용
                if (activeObjects.Count > 0)
                {
                    pooledObject = activeObjects[0];
                    ReturnObject(pooledObject);
                }
                else
                {
                    pooledObject = CreateNewPoolObject();
                }
            }
            
            // 타겟에 붙이기
            if (target != null)
            {
                pooledObject.transform.SetParent(target);
                pooledObject.transform.localPosition = Vector3.zero;
                pooledObject.transform.localRotation = Quaternion.identity;
                
                // 타겟 추적 컴포넌트 추가
                var targetTracker = pooledObject.GetComponent<EffectTargetTracker>();
                if (targetTracker == null)
                {
                    targetTracker = pooledObject.AddComponent<EffectTargetTracker>();
                }
                targetTracker.SetTarget(target);
            }
            else
            {
                // 타겟이 없을 경우 월드 위치 사용
                pooledObject.transform.SetParent(transform);
                pooledObject.transform.position = position;
                pooledObject.transform.rotation = rotation;
            }
            
            pooledObject.SetActive(true);
            activeObjects.Add(pooledObject);
            
            return pooledObject;
        }
        
        /// <summary>
        /// 풀로 오브젝트 반환
        /// </summary>
        public void ReturnObject(GameObject obj)
        {
            if (obj == null) return;
            
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            // 컴포넌트 초기화
            var animator = obj.GetComponent<SimpleEffectAnimator>();
            animator?.StopAnimation();
            
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
            }
            
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
        
        /// <summary>
        /// Texture2D 배열로 이펙트 재생 (타겟에 붙이기 옵션)
        /// </summary>
        public GameObject PlayTextureEffect(Vector3 position, Quaternion rotation, Sprite[] frames, float frameRate, bool loop, float duration, Transform target = null)
        {
            var effectObject = GetPooledObject(position, rotation, target);
            
            var animator = effectObject.GetComponent<SimpleEffectAnimator>();
            animator.PlayAnimation(frames, frameRate, loop, false); // destroyOnComplete = false (풀 사용)
            
            var poolable = effectObject.GetComponent<PoolableEffectObject>();
            poolable.StartReturnTimer(duration);
            
            return effectObject;
        }
        
        /// <summary>
        /// 프리팹 이펙트 재생 (기존 방식)
        /// </summary>
        public GameObject PlayPrefabEffect(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
        {
            var effectObject = Instantiate(prefab, position, rotation);
            Destroy(effectObject, duration);
            return effectObject;
        }
    }
}