using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 엘리트 몬스터 접사(Affix) 시스템
    /// 디아블로/PoE식 랜덤 접사 부여
    /// Champion(1접사), Rare(2접사), Unique(3접사+보스급)
    /// </summary>
    public class EliteModifierSystem : MonoBehaviour
    {
        public static EliteModifierSystem Instance { get; private set; }

        // 모든 접사 정의
        private static readonly List<EliteModifierData> allModifiers = new List<EliteModifierData>
        {
            // === 공격 접사 (7종) ===
            new EliteModifierData {
                type = EliteModifierType.Freezing,
                name = "빙결", namePrefix = "얼어붙은",
                description = "공격 시 빙결 확률",
                damageMultiplier = 1.0f, healthMultiplier = 1.2f, speedMultiplier = 0.9f,
                statusEffect = StatusType.Freeze, statusChance = 0.25f, statusDuration = 2f,
                glowColor = new Color(0.3f, 0.7f, 1f),
                expBonus = 1.5f, goldBonus = 1.3f, minFloor = 1
            },
            new EliteModifierData {
                type = EliteModifierType.Burning,
                name = "화염", namePrefix = "불타는",
                description = "공격 시 화상 부여",
                damageMultiplier = 1.15f, healthMultiplier = 1.1f, speedMultiplier = 1.0f,
                statusEffect = StatusType.Burn, statusChance = 0.3f, statusDuration = 4f,
                glowColor = new Color(1f, 0.3f, 0f),
                expBonus = 1.4f, goldBonus = 1.3f, minFloor = 1
            },
            new EliteModifierData {
                type = EliteModifierType.Poisonous,
                name = "맹독", namePrefix = "맹독의",
                description = "공격 시 독 부여 + 사망 시 독 구름",
                damageMultiplier = 1.0f, healthMultiplier = 1.15f, speedMultiplier = 1.0f,
                statusEffect = StatusType.Poison, statusChance = 0.35f, statusDuration = 6f,
                deathEffect = EliteDeathEffect.PoisonCloud,
                glowColor = new Color(0.2f, 0.8f, 0.1f),
                expBonus = 1.4f, goldBonus = 1.2f, minFloor = 2
            },
            new EliteModifierData {
                type = EliteModifierType.Electrified,
                name = "전기", namePrefix = "전류의",
                description = "주변에 전기장 생성, 감전",
                damageMultiplier = 1.1f, healthMultiplier = 1.1f, speedMultiplier = 1.05f,
                statusEffect = StatusType.Stun, statusChance = 0.15f, statusDuration = 1f,
                auraRange = 3f, auraDamage = 5f,
                glowColor = new Color(0.8f, 0.8f, 1f),
                expBonus = 1.5f, goldBonus = 1.4f, minFloor = 3
            },
            new EliteModifierData {
                type = EliteModifierType.Vampiric,
                name = "흡혈", namePrefix = "흡혈의",
                description = "데미지의 일부를 HP로 흡수",
                damageMultiplier = 1.1f, healthMultiplier = 1.3f, speedMultiplier = 1.0f,
                lifestealPercent = 0.2f,
                glowColor = new Color(0.6f, 0f, 0f),
                expBonus = 1.6f, goldBonus = 1.4f, minFloor = 3
            },
            new EliteModifierData {
                type = EliteModifierType.Berserking,
                name = "광폭", namePrefix = "광폭한",
                description = "HP 낮을수록 공격력/속도 증가",
                damageMultiplier = 1.0f, healthMultiplier = 1.0f, speedMultiplier = 1.0f,
                enrageThreshold = 0.5f, enrageDamageBonus = 0.5f, enrageSpeedBonus = 0.3f,
                glowColor = new Color(1f, 0f, 0f),
                expBonus = 1.5f, goldBonus = 1.3f, minFloor = 2
            },
            new EliteModifierData {
                type = EliteModifierType.ChainLightning,
                name = "연쇄번개", namePrefix = "번개사슬의",
                description = "공격이 주변 적(플레이어)에게 연쇄",
                damageMultiplier = 1.05f, healthMultiplier = 1.2f, speedMultiplier = 1.0f,
                chainCount = 3, chainDamageReduction = 0.3f,
                glowColor = new Color(1f, 1f, 0.3f),
                expBonus = 1.6f, goldBonus = 1.5f, minFloor = 4
            },

            // === 방어 접사 (5종) ===
            new EliteModifierData {
                type = EliteModifierType.Shielded,
                name = "보호막", namePrefix = "보호막의",
                description = "일정 시간마다 피해 흡수 보호막 생성",
                damageMultiplier = 1.0f, healthMultiplier = 1.4f, speedMultiplier = 0.95f,
                shieldAmount = 0.2f, shieldCooldown = 15f,
                glowColor = new Color(0.3f, 0.3f, 1f),
                expBonus = 1.5f, goldBonus = 1.3f, minFloor = 2
            },
            new EliteModifierData {
                type = EliteModifierType.Reflective,
                name = "반사", namePrefix = "반사의",
                description = "받은 피해의 일부를 공격자에게 반사",
                damageMultiplier = 0.9f, healthMultiplier = 1.5f, speedMultiplier = 1.0f,
                reflectPercent = 0.15f,
                glowColor = new Color(0.7f, 0.7f, 0.9f),
                expBonus = 1.5f, goldBonus = 1.4f, minFloor = 4
            },
            new EliteModifierData {
                type = EliteModifierType.Regenerating,
                name = "재생", namePrefix = "재생의",
                description = "지속적으로 HP 회복",
                damageMultiplier = 1.05f, healthMultiplier = 1.3f, speedMultiplier = 1.0f,
                regenPercent = 0.01f,
                glowColor = new Color(0.2f, 1f, 0.2f),
                expBonus = 1.4f, goldBonus = 1.3f, minFloor = 2
            },
            new EliteModifierData {
                type = EliteModifierType.Armored,
                name = "중갑", namePrefix = "중갑의",
                description = "물리 방어력 대폭 증가",
                damageMultiplier = 1.0f, healthMultiplier = 1.2f, speedMultiplier = 0.85f,
                defenseMultiplier = 2.0f,
                glowColor = new Color(0.5f, 0.5f, 0.5f),
                expBonus = 1.3f, goldBonus = 1.2f, minFloor = 1
            },
            new EliteModifierData {
                type = EliteModifierType.MagicResistant,
                name = "마법저항", namePrefix = "마법저항의",
                description = "마법 방어력 대폭 증가",
                damageMultiplier = 1.0f, healthMultiplier = 1.2f, speedMultiplier = 1.0f,
                magicDefenseMultiplier = 2.0f,
                glowColor = new Color(0.4f, 0.2f, 0.8f),
                expBonus = 1.3f, goldBonus = 1.2f, minFloor = 3
            },

            // === 특수 접사 (8종) ===
            new EliteModifierData {
                type = EliteModifierType.Teleporting,
                name = "순간이동", namePrefix = "공간의",
                description = "일정 시간마다 플레이어 근처로 텔레포트",
                damageMultiplier = 1.1f, healthMultiplier = 1.2f, speedMultiplier = 1.0f,
                teleportCooldown = 8f, teleportRange = 5f,
                glowColor = new Color(0.5f, 0f, 1f),
                expBonus = 1.5f, goldBonus = 1.4f, minFloor = 3
            },
            new EliteModifierData {
                type = EliteModifierType.Summoner,
                name = "소환", namePrefix = "소환사",
                description = "주기적으로 하급 몬스터 소환",
                damageMultiplier = 0.85f, healthMultiplier = 1.5f, speedMultiplier = 0.9f,
                summonCount = 2, summonCooldown = 20f,
                glowColor = new Color(0.8f, 0.4f, 0f),
                expBonus = 1.7f, goldBonus = 1.5f, minFloor = 4
            },
            new EliteModifierData {
                type = EliteModifierType.Splitting,
                name = "분열", namePrefix = "분열하는",
                description = "사망 시 약한 분신 2체로 분열",
                damageMultiplier = 1.0f, healthMultiplier = 1.0f, speedMultiplier = 1.0f,
                deathEffect = EliteDeathEffect.Split,
                splitCount = 2, splitHealthPercent = 0.3f,
                glowColor = new Color(0.6f, 0.9f, 0.6f),
                expBonus = 1.6f, goldBonus = 1.5f, minFloor = 5
            },
            new EliteModifierData {
                type = EliteModifierType.Exploding,
                name = "자폭", namePrefix = "폭발하는",
                description = "사망 시 폭발하여 주변에 큰 피해",
                damageMultiplier = 1.0f, healthMultiplier = 0.9f, speedMultiplier = 1.1f,
                deathEffect = EliteDeathEffect.Explode,
                explosionDamage = 80f, explosionRadius = 3f,
                glowColor = new Color(1f, 0.5f, 0f),
                expBonus = 1.4f, goldBonus = 1.3f, minFloor = 2
            },
            new EliteModifierData {
                type = EliteModifierType.Hasting,
                name = "가속", namePrefix = "쾌속의",
                description = "이동/공격 속도 대폭 증가",
                damageMultiplier = 0.9f, healthMultiplier = 0.9f, speedMultiplier = 1.5f,
                attackSpeedMultiplier = 1.4f,
                glowColor = new Color(0.9f, 0.9f, 0f),
                expBonus = 1.4f, goldBonus = 1.3f, minFloor = 1
            },
            new EliteModifierData {
                type = EliteModifierType.SoulDrain,
                name = "영혼흡수", namePrefix = "영혼포식자",
                description = "공격 시 MP 흡수",
                damageMultiplier = 1.05f, healthMultiplier = 1.2f, speedMultiplier = 1.0f,
                manaDrainPercent = 0.1f,
                glowColor = new Color(0.2f, 0f, 0.5f),
                expBonus = 1.5f, goldBonus = 1.4f, minFloor = 5
            },
            new EliteModifierData {
                type = EliteModifierType.TimeWarp,
                name = "시간왜곡", namePrefix = "시간의",
                description = "주변 플레이어 이동/공격 속도 감소",
                damageMultiplier = 1.0f, healthMultiplier = 1.3f, speedMultiplier = 1.0f,
                auraRange = 4f, auraSlowPercent = 0.25f,
                glowColor = new Color(0.5f, 0.5f, 0.8f),
                expBonus = 1.6f, goldBonus = 1.5f, minFloor = 6
            },
            new EliteModifierData {
                type = EliteModifierType.Cloning,
                name = "분신", namePrefix = "분신의",
                description = "전투 중 환영 분신 생성 (약한 복제)",
                damageMultiplier = 1.0f, healthMultiplier = 1.1f, speedMultiplier = 1.0f,
                cloneCooldown = 25f, cloneHealthPercent = 0.2f,
                glowColor = new Color(0.7f, 0.3f, 0.7f),
                expBonus = 1.5f, goldBonus = 1.4f, minFloor = 7
            },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 엘리트 등급에 맞는 랜덤 접사 생성
        /// </summary>
        public List<EliteModifierData> GenerateModifiers(EliteGrade grade, int currentFloor)
        {
            int count = grade switch
            {
                EliteGrade.Champion => 1,
                EliteGrade.Rare => 2,
                EliteGrade.Unique => 3,
                _ => 0
            };

            if (count == 0) return new List<EliteModifierData>();

            var available = allModifiers.Where(m => m.minFloor <= currentFloor).ToList();
            var selected = new List<EliteModifierData>();

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            return selected;
        }

        /// <summary>
        /// 접사가 적용된 엘리트 이름 생성
        /// </summary>
        public static string GenerateEliteName(string baseName, List<EliteModifierData> modifiers, EliteGrade grade)
        {
            string prefix = "";
            foreach (var mod in modifiers)
            {
                prefix += mod.namePrefix + " ";
            }

            string gradePrefix = grade switch
            {
                EliteGrade.Champion => "[챔피언] ",
                EliteGrade.Rare => "[레어] ",
                EliteGrade.Unique => "[유니크] ",
                _ => ""
            };

            return $"{gradePrefix}{prefix}{baseName}";
        }

        /// <summary>
        /// 접사 적용된 스탯 계산
        /// </summary>
        public static float CalculateHealth(float baseHealth, List<EliteModifierData> modifiers, EliteGrade grade)
        {
            float mult = grade switch
            {
                EliteGrade.Champion => 2f,
                EliteGrade.Rare => 3.5f,
                EliteGrade.Unique => 6f,
                _ => 1f
            };

            foreach (var mod in modifiers)
                mult *= mod.healthMultiplier;

            return baseHealth * mult;
        }

        public static float CalculateDamage(float baseDamage, List<EliteModifierData> modifiers, EliteGrade grade)
        {
            float mult = grade switch
            {
                EliteGrade.Champion => 1.3f,
                EliteGrade.Rare => 1.6f,
                EliteGrade.Unique => 2.0f,
                _ => 1f
            };

            foreach (var mod in modifiers)
                mult *= mod.damageMultiplier;

            return baseDamage * mult;
        }

        public static float CalculateSpeed(float baseSpeed, List<EliteModifierData> modifiers)
        {
            float mult = 1f;
            foreach (var mod in modifiers)
                mult *= mod.speedMultiplier;

            return baseSpeed * mult;
        }

        /// <summary>
        /// 접사 적용된 보상 배율
        /// </summary>
        public static float CalculateExpBonus(List<EliteModifierData> modifiers, EliteGrade grade)
        {
            float base_ = grade switch
            {
                EliteGrade.Champion => 2f,
                EliteGrade.Rare => 4f,
                EliteGrade.Unique => 8f,
                _ => 1f
            };

            foreach (var mod in modifiers)
                base_ *= mod.expBonus;

            return base_;
        }

        public static float CalculateGoldBonus(List<EliteModifierData> modifiers, EliteGrade grade)
        {
            float base_ = grade switch
            {
                EliteGrade.Champion => 1.5f,
                EliteGrade.Rare => 3f,
                EliteGrade.Unique => 6f,
                _ => 1f
            };

            foreach (var mod in modifiers)
                base_ *= mod.goldBonus;

            return base_;
        }

        /// <summary>
        /// 접사 리스트 조회
        /// </summary>
        public static List<EliteModifierData> GetAllModifiers()
        {
            return allModifiers;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    // === 엘리트 등급 ===

    public enum EliteGrade
    {
        Normal,         // 일반 몬스터
        Champion,       // 챔피언 (접사 1개)
        Rare,           // 레어 (접사 2개)
        Unique          // 유니크 (접사 3개, 보스급)
    }

    // === 접사 타입 ===

    public enum EliteModifierType
    {
        // 공격
        Freezing,       // 빙결
        Burning,        // 화염
        Poisonous,      // 맹독
        Electrified,    // 전기
        Vampiric,       // 흡혈
        Berserking,     // 광폭
        ChainLightning, // 연쇄번개

        // 방어
        Shielded,       // 보호막
        Reflective,     // 반사
        Regenerating,   // 재생
        Armored,        // 중갑
        MagicResistant, // 마법저항

        // 특수
        Teleporting,    // 순간이동
        Summoner,       // 소환
        Splitting,      // 분열 (사망시)
        Exploding,      // 폭발 (사망시)
        Hasting,        // 가속
        SoulDrain,      // 영혼흡수
        TimeWarp,       // 시간왜곡
        Cloning         // 분신
    }

    public enum EliteDeathEffect
    {
        None,
        Explode,        // 폭발
        PoisonCloud,    // 독 구름
        Split,          // 분열
        DropMinions     // 하급 몬스터 소환
    }

    // === 접사 데이터 ===

    [System.Serializable]
    public class EliteModifierData
    {
        public EliteModifierType type;
        public string name;
        public string namePrefix;
        public string description;

        // 기본 스탯 배율
        public float damageMultiplier = 1f;
        public float healthMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float defenseMultiplier = 1f;
        public float magicDefenseMultiplier = 1f;
        public float attackSpeedMultiplier = 1f;

        // 상태이상
        public StatusType statusEffect = StatusType.None;
        public float statusChance = 0f;
        public float statusDuration = 0f;

        // 특수 능력
        public float lifestealPercent = 0f;
        public float reflectPercent = 0f;
        public float regenPercent = 0f;
        public float manaDrainPercent = 0f;

        // 오라 효과
        public float auraRange = 0f;
        public float auraDamage = 0f;
        public float auraSlowPercent = 0f;

        // 광폭
        public float enrageThreshold = 0f;
        public float enrageDamageBonus = 0f;
        public float enrageSpeedBonus = 0f;

        // 보호막
        public float shieldAmount = 0f;
        public float shieldCooldown = 0f;

        // 순간이동
        public float teleportCooldown = 0f;
        public float teleportRange = 0f;

        // 소환
        public int summonCount = 0;
        public float summonCooldown = 0f;

        // 분열
        public EliteDeathEffect deathEffect = EliteDeathEffect.None;
        public int splitCount = 0;
        public float splitHealthPercent = 0f;

        // 폭발
        public float explosionDamage = 0f;
        public float explosionRadius = 0f;

        // 연쇄
        public int chainCount = 0;
        public float chainDamageReduction = 0f;

        // 분신
        public float cloneCooldown = 0f;
        public float cloneHealthPercent = 0f;

        // 시각
        public Color glowColor = Color.white;

        // 보상 배율
        public float expBonus = 1f;
        public float goldBonus = 1f;

        // 최소 층 제한
        public int minFloor = 1;
    }
}
