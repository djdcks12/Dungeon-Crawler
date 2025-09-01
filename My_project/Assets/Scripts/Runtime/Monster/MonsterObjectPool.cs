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
            rigidBody.bodyType = RigidbodyType2D.Dynamic; // AI 이동을 위해 Dynamic으로 변경
            rigidBody.freezeRotation = true; // 회전 방지 (스케일 플립만 사용)
            
            var collider = monsterObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;
            
            // SpriteRenderer 추가
            var spriteRenderer = monsterObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 0;
            
            // 몬스터 핵심 컴포넌트들
            var monsterEntity = monsterObject.AddComponent<MonsterEntity>();
            var monsterAI = monsterObject.AddComponent<MonsterAI>();
            
            // 몬스터 애니메이션 시스템
            var animationController = monsterObject.AddComponent<MonsterSpriteAnimator>();

            monsterObject.AddComponent<MonsterSkillSystem>();
            monsterObject.AddComponent<MonsterSoulDropSystem>();
            
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
                spriteRenderer.sortingLayerName = "PlayerOrMonster";
            }
            
            // 모든 컴포넌트 명시적 초기화
            InitializeAllComponents(monster);
            
        }

        /// <summary>
        /// 모든 컴포넌트 초기화
        /// </summary>
        private void InitializeAllComponents(GameObject monster)
        {
            Debug.Log($"🔧 Initializing all components for {monster.name}");

            // MonsterAI 초기화
            var monsterAI = monster.GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.InitializeAI();
            }
            var monsterSkillSystem = monster.GetComponent<MonsterSkillSystem>();
            if (monsterSkillSystem != null)
            {
                monsterSkillSystem.InitializeSkillSystem();
            }
        }
        
        /// <summary>
        /// 모든 컴포넌트 정리
        /// </summary>
        private void CleanupAllComponents(GameObject monster)
        {
            Debug.Log($"🧹 Cleaning up all components for {monster.name}");
            
            // MonsterAI 정리
            var monsterAI = monster.GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.CleanupAI();
            }
            var monsterSkillSystem = monster.GetComponent<MonsterSkillSystem>();
            if (monsterSkillSystem != null)
            {
                monsterSkillSystem.CleanupSkillSystem();
            }
            
            // MonsterEntity 정리
                var monsterEntity = monster.GetComponent<MonsterEntity>();
            if (monsterEntity != null)
            {
                monsterEntity.ResetEntity();
            }
            
            // MonsterSpriteAnimator 정리
            var animationController = monster.GetComponent<MonsterSpriteAnimator>();
            if (animationController != null)
            {
                animationController.StopAllAnimations();
            }
        }
        
        /// <summary>
        /// 풀로 몬스터 반환 (NetworkObject 제거)
        /// </summary>
        public void ReturnMonster(GameObject monster)
        {
            if (monster == null) return;
            
            // NetworkObject가 있다면 Despawn 후 제거
            var networkObject = monster.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                try 
                {
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn(false); // destroy=false
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Failed to despawn NetworkObject: {e.Message}");
                }
                
                // NetworkObject 컴포넌트 제거
                DestroyImmediate(networkObject);
                Debug.Log($"🔧 Removed NetworkObject from {monster.name}");
            }
            
            monster.SetActive(false);
            monster.transform.SetParent(transform);
            
            // 모든 컴포넌트 명시적 정리
            CleanupAllComponents(monster);
            
            activeMonsters.Remove(monster);
            availableMonsters.Enqueue(monster);
            
            Debug.Log($"♻️ Monster returned to pool: {monster.name}");
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