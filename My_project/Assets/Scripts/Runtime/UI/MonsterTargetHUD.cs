using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 마지막으로 공격한 몬스터의 HP를 화면 상단에 표시하는 UI
    /// 몬스터가 죽으면 자동으로 사라짐
    /// </summary>
    public class MonsterTargetHUD : MonoBehaviour
    {
        [Header("Monster HP UI")]
        [SerializeField] private GameObject monsterHPPanel;
        [SerializeField] private Slider monsterHealthSlider;
        [SerializeField] private Text monsterNameText;
        [SerializeField] private Text monsterHealthText;
        [SerializeField] private Text monsterLevelText;
        
        [Header("Settings")]
        [SerializeField] private float autoHideDelay = 5f; // 5초 후 자동 숨김
        
        // 현재 타겟팅된 몬스터
        private MonsterEntity currentTarget;
        private float lastDamageTime;
        private bool isSubscribed = false;
        
        private void Awake()
        {
            // 초기에는 숨김
            HideMonsterHP();
        }
        
        private void Start()
        {
            // CombatSystem의 이벤트에 구독하여 몬스터 공격 감지
            SubscribeToCombatEvents();
        }
        
        private void Update()
        {
            // 현재 타겟이 있고 일정 시간이 지나면 자동 숨김
            if (currentTarget != null && Time.time - lastDamageTime > autoHideDelay)
            {
                HideMonsterHP();
            }
            
            // 현재 타겟이 죽었으면 숨김
            if (currentTarget != null && currentTarget.IsDead)
            {
                HideMonsterHP();
            }
        }
        
        /// <summary>
        /// CombatSystem 이벤트 구독
        /// </summary>
        private void SubscribeToCombatEvents()
        {
            // 로컬 플레이어의 CombatSystem 찾기
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localPlayer != null)
                {
                    var combatSystem = localPlayer.GetComponent<CombatSystem>();
                    if (combatSystem != null)
                    {
                        // 몬스터 공격 시 이벤트 구독
                        combatSystem.OnMonsterAttacked += OnMonsterAttacked;
                        isSubscribed = true;
                        Debug.Log("✅ MonsterTargetHUD subscribed to CombatSystem events");
                        return;
                    }
                }
            }
            
            // 로컬 플레이어를 찾지 못하면 재시도
            if (!isSubscribed)
            {
                Invoke(nameof(SubscribeToCombatEvents), 0.5f);
            }
        }
        
        /// <summary>
        /// 몬스터 공격 이벤트 처리
        /// </summary>
        private void OnMonsterAttacked(MonsterEntity monster, float damage)
        {
            if (monster == null || monster.IsDead) return;
            
            // 새로운 타겟이거나 기존 타겟과 다르면 변경
            if (currentTarget != monster)
            {
                SetNewTarget(monster);
            }
            
            // HP UI 업데이트
            UpdateMonsterHP();
            lastDamageTime = Time.time;
        }
        
        /// <summary>
        /// 새로운 타겟 설정
        /// </summary>
        private void SetNewTarget(MonsterEntity monster)
        {
            // 기존 타겟의 이벤트 구독 해제
            if (currentTarget != null)
            {
                UnsubscribeFromMonster(currentTarget);
            }
            
            // 새로운 타겟 설정
            currentTarget = monster;
            
            if (currentTarget != null)
            {
                // 몬스터 이벤트 구독
                SubscribeToMonster(currentTarget);
                
                // UI 표시
                ShowMonsterHP();
                UpdateMonsterInfo();
                UpdateMonsterHP();
                
                Debug.Log($"🎯 New target: {currentTarget.VariantData?.variantName ?? "Unknown Monster"}");
            }
        }
        
        /// <summary>
        /// 몬스터 이벤트 구독
        /// </summary>
        private void SubscribeToMonster(MonsterEntity monster)
        {
            if (monster != null)
            {
                monster.OnDamageTaken += OnTargetDamageTaken;
                monster.OnDeath += OnTargetDeath;
            }
        }
        
        /// <summary>
        /// 몬스터 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromMonster(MonsterEntity monster)
        {
            if (monster != null)
            {
                monster.OnDamageTaken -= OnTargetDamageTaken;
                monster.OnDeath -= OnTargetDeath;
            }
        }
        
        /// <summary>
        /// 타겟 몬스터가 데미지를 받았을 때
        /// </summary>
        private void OnTargetDamageTaken(float damage)
        {
            UpdateMonsterHP();
            lastDamageTime = Time.time;
        }
        
        /// <summary>
        /// 타겟 몬스터가 죽었을 때
        /// </summary>
        private void OnTargetDeath()
        {
            Debug.Log($"🪦 Target monster died: {currentTarget?.VariantData?.variantName ?? "Unknown"}");
            HideMonsterHP();
        }
        
        /// <summary>
        /// 몬스터 HP UI 표시
        /// </summary>
        private void ShowMonsterHP()
        {
            if (monsterHPPanel != null)
            {
                monsterHPPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// 몬스터 HP UI 숨김
        /// </summary>
        private void HideMonsterHP()
        {
            if (monsterHPPanel != null)
            {
                monsterHPPanel.SetActive(false);
            }
            
            // 기존 타겟 정리
            if (currentTarget != null)
            {
                UnsubscribeFromMonster(currentTarget);
                currentTarget = null;
            }
        }
        
        /// <summary>
        /// 몬스터 정보 업데이트 (이름, 레벨 등)
        /// </summary>
        private void UpdateMonsterInfo()
        {
            if (currentTarget == null) return;
            
            // 몬스터 이름
            string monsterName = currentTarget.VariantData?.variantName ?? "Unknown Monster";
            SetText(monsterNameText, monsterName);
            
            // 몬스터 등급/레벨 (Grade를 레벨로 표시)
            int displayLevel = Mathf.RoundToInt(currentTarget.Grade);
            SetText(monsterLevelText, $"Lv.{displayLevel}");
        }
        
        /// <summary>
        /// 몬스터 HP 업데이트
        /// </summary>
        private void UpdateMonsterHP()
        {
            if (currentTarget == null) return;
            
            float currentHP = currentTarget.CurrentHP;
            float maxHP = currentTarget.MaxHP;
            
            // HP 슬라이더 업데이트
            if (monsterHealthSlider != null)
            {
                monsterHealthSlider.maxValue = maxHP;
                monsterHealthSlider.value = currentHP;
                
                // HP에 따른 색상 변경
                var fillImage = monsterHealthSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    float healthPercent = maxHP > 0 ? currentHP / maxHP : 0f;
                    if (healthPercent <= 0.25f)
                        fillImage.color = Color.red;
                    else if (healthPercent <= 0.5f)
                        fillImage.color = new Color(1f, 0.5f, 0f); // 주황색
                    else
                        fillImage.color = Color.red; // 몬스터는 기본적으로 빨간색
                }
            }
            
            // HP 텍스트 업데이트
            SetText(monsterHealthText, $"{currentHP:F0} / {maxHP:F0}");
        }
        
        /// <summary>
        /// 안전한 텍스트 설정
        /// </summary>
        private void SetText(Text textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        
        /// <summary>
        /// 현재 타겟이 있는지 확인
        /// </summary>
        public bool HasTarget()
        {
            return currentTarget != null && !currentTarget.IsDead;
        }
        
        /// <summary>
        /// 현재 타겟 반환
        /// </summary>
        public MonsterEntity GetCurrentTarget()
        {
            return currentTarget;
        }
        
        /// <summary>
        /// 수동으로 타겟 설정 (다른 시스템에서 호출 가능)
        /// </summary>
        public void SetTarget(MonsterEntity monster)
        {
            if (monster != null && !monster.IsDead)
            {
                SetNewTarget(monster);
                lastDamageTime = Time.time;
            }
        }
        
        /// <summary>
        /// 수동으로 타겟 해제
        /// </summary>
        public void ClearTarget()
        {
            HideMonsterHP();
        }
        
        /// <summary>
        /// 컴포넌트 정리
        /// </summary>
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (currentTarget != null)
            {
                UnsubscribeFromMonster(currentTarget);
            }
            
            // CombatSystem 이벤트 구독 해제는 자동으로 처리됨 (OnDestroy에서)
        }
        
        /// <summary>
        /// 디버그 정보 표시
        /// </summary>
        [ContextMenu("Show Debug Info")]
        private void ShowDebugInfo()
        {
            Debug.Log($"=== MonsterTargetHUD Debug Info ===");
            Debug.Log($"Current Target: {(currentTarget != null ? currentTarget.VariantData?.variantName : "None")}");
            Debug.Log($"Is Subscribed: {isSubscribed}");
            Debug.Log($"Panel Active: {(monsterHPPanel != null ? monsterHPPanel.activeInHierarchy : false)}");
            Debug.Log($"Last Damage Time: {lastDamageTime}");
        }
    }
}