using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 오브젝트 풀 관리자 - 동적 몬스터 생성
    /// </summary>
    public class MonsterObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 30;
        
        private Queue<GameObject> availableMonsters = new Queue<GameObject>();
        private List<GameObject> activeMonsters = new List<GameObject>();
        
        public static MonsterObjectPool Instance { get; private set; }
        
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
        
        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPoolMonster();
            }
        }
        
        /// <summary>
        /// 새로운 풀 몬스터 생성 (GoblinNormal 기반)
        /// </summary>
        private GameObject CreateNewPoolMonster()
        {
            var monsterObject = new GameObject("PooledMonster");
            monsterObject.SetActive(false);
            monsterObject.transform.SetParent(transform);
            
            // 기본 컴포넌트들 추가 (GoblinNormal 기반)
            var rigidBody = monsterObject.AddComponent<Rigidbody2D>();
            rigidBody.gravityScale = 0f; // 2D 탑다운이므로 중력 없음
            
            var collider = monsterObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            
            // SpriteRenderer 추가
            var spriteRenderer = monsterObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 0;
            
            // 네트워크 컴포넌트들
            var networkObject = monsterObject.AddComponent<NetworkObject>();
            
            // 몬스터 핵심 컴포넌트들
            var monsterEntity = monsterObject.AddComponent<MonsterEntity>();
            var monsterAI = monsterObject.AddComponent<MonsterAI>();
            
            // 몬스터 애니메이션 시스템
            var animationController = monsterObject.AddComponent<MonsterSpriteAnimator>();
            
            // 풀 관리 컴포넌트
            var poolable = monsterObject.AddComponent<PoolableMonster>();
            poolable.OnReturnToPool += ReturnMonster;
            
            availableMonsters.Enqueue(monsterObject);
            return monsterObject;
        }
        
        /// <summary>
        /// 풀에서 몬스터 가져오기
        /// </summary>
        public GameObject GetPooledMonster(Vector3 position, MonsterVariantData variantData)
        {
            GameObject pooledMonster;
            
            if (availableMonsters.Count > 0)
            {
                pooledMonster = availableMonsters.Dequeue();
            }
            else if (activeMonsters.Count < maxPoolSize)
            {
                pooledMonster = CreateNewPoolMonster();
            }
            else
            {
                // 가장 오래된 활성 몬스터 재사용
                pooledMonster = activeMonsters[0];
                ReturnMonster(pooledMonster);
                pooledMonster = availableMonsters.Dequeue();
            }

            // 몬스터 설정
            pooledMonster.SetActive(true);
            SetupMonster(pooledMonster, position, variantData);
            
            
            activeMonsters.Add(pooledMonster);
            
            return pooledMonster;
        }

        /// <summary>
        /// 몬스터 설정 (variant data 기반)
        /// </summary>
        private void SetupMonster(GameObject monster, Vector3 position, MonsterVariantData variantData)
        {
            // 위치 설정
            monster.transform.position = position;
            monster.transform.rotation = Quaternion.identity;
            monster.transform.localScale = new Vector3(5, 5, 1);

            // 몬스터 엔티티 설정
            var monsterEntity = monster.GetComponent<MonsterEntity>();
            if (monsterEntity != null)
            {
                monsterEntity.SetVariantData(variantData);
            }

            // 스프라이트 애니메이션 설정
            var animationController = monster.GetComponent<MonsterSpriteAnimator>();
            if (animationController != null && variantData != null)
            {
                animationController.SetupAnimations(variantData);
            }

            // 기본 스프라이트 설정 (idle 상태)
            var spriteRenderer = monster.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && variantData != null && variantData.IdleSprites != null && variantData.IdleSprites.Length > 0)
            {
                spriteRenderer.sprite = variantData.IdleSprites[0];
            }

            spriteRenderer.sortingLayerName = "PlayerOrMonster";
            
        }
        
        /// <summary>
        /// 풀로 몬스터 반환
        /// </summary>
        public void ReturnMonster(GameObject monster)
        {
            if (monster == null) return;
            
            monster.SetActive(false);
            monster.transform.SetParent(transform);
            
            // 몬스터 컴포넌트 초기화
            var monsterEntity = monster.GetComponent<MonsterEntity>();
            monsterEntity?.ResetEntity();
            
            var monsterAI = monster.GetComponent<MonsterAI>();
            //monsterAI?.ResetAI();
            
            var animationController = monster.GetComponent<MonsterSpriteAnimator>();
            animationController?.StopAllAnimations();
            
            activeMonsters.Remove(monster);
            availableMonsters.Enqueue(monster);
        }
        
        /// <summary>
        /// 모든 활성 몬스터를 풀로 반환
        /// </summary>
        public void ReturnAllMonsters()
        {
            var monstersToReturn = new List<GameObject>(activeMonsters);
            foreach (var monster in monstersToReturn)
            {
                ReturnMonster(monster);
            }
        }
        
        /// <summary>
        /// 풀 상태 정보
        /// </summary>
        public void LogPoolStatus()
        {
            Debug.Log($"Monster Pool Status - Available: {availableMonsters.Count}, Active: {activeMonsters.Count}, Total: {availableMonsters.Count + activeMonsters.Count}");
        }
    }
}