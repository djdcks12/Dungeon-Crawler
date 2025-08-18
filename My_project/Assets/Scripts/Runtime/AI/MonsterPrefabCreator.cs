using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 몬스터 프리팹 생성 및 설정 도우미
    /// 에디터에서 쉽게 몬스터 프리팹을 만들 수 있도록 도움
    /// </summary>
    public class MonsterPrefabCreator : MonoBehaviour
    {
        [Header("몬스터 기본 설정")]
        [SerializeField] private string monsterName = "Basic Monster";
        [SerializeField] private int baseLevel = 1;
        [SerializeField] private MonsterAIType aiType = MonsterAIType.Aggressive;
        [SerializeField] private Color monsterColor = Color.red;
        
        [Header("체력 설정")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private long expReward = 50;
        
        [Header("AI 설정")]
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackDamage = 20f;
        
        [Header("시각적 설정")]
        [SerializeField] private Vector2 spriteSize = Vector2.one;
        [SerializeField] private bool createSprite = true;
        
        /// <summary>
        /// 몬스터 프리팹 설정 (에디터에서 호출)
        /// </summary>
        [ContextMenu("Setup Monster Prefab")]
        public void SetupMonsterPrefab()
        {
            // 기본 컴포넌트들 추가
            SetupBasicComponents();
            SetupNetworking();
            SetupPhysics();
            SetupVisuals();
            SetupAI();
            SetupHealth();
            
            Debug.Log($"✅ Monster prefab '{monsterName}' setup completed!");
        }
        
        /// <summary>
        /// 기본 컴포넌트 설정
        /// </summary>
        private void SetupBasicComponents()
        {
            gameObject.name = monsterName;
            
            // 태그와 레이어 설정
            gameObject.tag = "Enemy";
            gameObject.layer = LayerMask.NameToLayer("Enemy");
            
            if (gameObject.layer == -1)
            {
                Debug.LogWarning("Enemy layer not found. Please create Enemy layer in Project Settings.");
            }
        }
        
        /// <summary>
        /// 네트워크 설정
        /// </summary>
        private void SetupNetworking()
        {
            if (GetComponent<NetworkObject>() == null)
            {
                var networkObject = gameObject.AddComponent<NetworkObject>();
                networkObject.DontDestroyWithOwner = false;
                networkObject.AutoObjectParentSync = false;
            }
        }
        
        /// <summary>
        /// 물리 시스템 설정
        /// </summary>
        private void SetupPhysics()
        {
            // Rigidbody2D
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.gravityScale = 0f; // 탑다운 뷰이므로 중력 비활성화
            rb.linearDamping = 2f; // 이동 시 부드러운 정지
            
            // Collider2D
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.4f;
                circleCollider.isTrigger = false;
            }
        }
        
        /// <summary>
        /// 시각적 설정
        /// </summary>
        private void SetupVisuals()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            if (createSprite)
            {
                // 기본 몬스터 스프라이트 생성
                spriteRenderer.sprite = CreateMonsterSprite();
            }
            
            spriteRenderer.color = monsterColor;
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 1;
        }
        
        /// <summary>
        /// AI 시스템 설정
        /// </summary>
        private void SetupAI()
        {
            var monsterAI = GetComponent<MonsterAI>();
            if (monsterAI == null)
            {
                monsterAI = gameObject.AddComponent<MonsterAI>();
            }
            
            // MonsterAI 설정 값들을 직접 적용
            monsterAI.SetAIType(aiType);
            
            // 필드 값들은 public이나 SetAIType을 통해 설정되므로 
            // SerializedObject 대신 직접 접근하거나 public 메서드 사용
        }
        
        /// <summary>
        /// 체력 시스템 설정
        /// </summary>
        private void SetupHealth()
        {
            var monsterHealth = GetComponent<MonsterHealth>();
            if (monsterHealth == null)
            {
                monsterHealth = gameObject.AddComponent<MonsterHealth>();
            }
            
            // MonsterHealth 설정값들을 직접 적용
            monsterHealth.SetMonsterInfo(monsterName, baseLevel, "Prefab", maxHealth, expReward);
        }
        
        /// <summary>
        /// 기본 몬스터 스프라이트 생성
        /// </summary>
        private Sprite CreateMonsterSprite()
        {
            int width = Mathf.RoundToInt(spriteSize.x * 32);
            int height = Mathf.RoundToInt(spriteSize.y * 32);
            
            Texture2D texture = new Texture2D(width, height);
            
            // 간단한 원형 몬스터 생성
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float radius = Mathf.Min(width, height) * 0.4f;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= radius)
                    {
                        // 몬스터 내부
                        float alpha = 1f - (distance / radius) * 0.3f;
                        texture.SetPixel(x, y, new Color(monsterColor.r, monsterColor.g, monsterColor.b, alpha));
                    }
                    else if (distance <= radius + 2f)
                    {
                        // 테두리
                        texture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        // 투명
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 몬스터 타입별 프리셋 적용
        /// </summary>
        [ContextMenu("Apply Goblin Preset")]
        public void ApplyGoblinPreset()
        {
            monsterName = "Goblin";
            aiType = MonsterAIType.Aggressive;
            monsterColor = Color.green;
            maxHealth = 80f;
            expReward = 40;
            detectionRange = 4f;
            attackRange = 1.2f;
            moveSpeed = 2.5f;
            attackDamage = 15f;
            
            SetupMonsterPrefab();
        }
        
        [ContextMenu("Apply Orc Preset")]
        public void ApplyOrcPreset()
        {
            monsterName = "Orc";
            aiType = MonsterAIType.Territorial;
            monsterColor = Color.red;
            maxHealth = 150f;
            expReward = 75;
            detectionRange = 5f;
            attackRange = 1.8f;
            moveSpeed = 1.8f;
            attackDamage = 30f;
            
            SetupMonsterPrefab();
        }
        
        [ContextMenu("Apply Slime Preset")]
        public void ApplySlimePreset()
        {
            monsterName = "Slime";
            aiType = MonsterAIType.Passive;
            monsterColor = Color.blue;
            maxHealth = 60f;
            expReward = 25;
            detectionRange = 2f;
            attackRange = 1f;
            moveSpeed = 1.5f;
            attackDamage = 10f;
            
            SetupMonsterPrefab();
        }
        
        [ContextMenu("Apply Skeleton Preset")]
        public void ApplySkeletonPreset()
        {
            monsterName = "Skeleton";
            aiType = MonsterAIType.Defensive;
            monsterColor = Color.white;
            maxHealth = 120f;
            expReward = 60;
            detectionRange = 6f;
            attackRange = 2f;
            moveSpeed = 2.2f;
            attackDamage = 25f;
            
            SetupMonsterPrefab();
        }
    }
}