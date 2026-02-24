using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 월드보스 데이터 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "New World Boss", menuName = "Dungeon Crawler/World Boss Data")]
    public class WorldBossData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string bossId;
        [SerializeField] private string bossName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite bossIcon;

        [Header("스탯")]
        [SerializeField] private long maxHP = 1000000;
        [SerializeField] private int level = 15;
        [SerializeField] private float attackDamage = 200f;
        [SerializeField] private float defense = 50f;
        [SerializeField] private float magicDefense = 50f;

        [Header("페이즈 (HP% 기준)")]
        [SerializeField] private float phase2Threshold = 0.75f;
        [SerializeField] private float phase3Threshold = 0.50f;
        [SerializeField] private float enrageThreshold = 0.25f;

        [Header("타이머")]
        [SerializeField] private float spawnInterval = 1800f; // 30분
        [SerializeField] private float despawnTimeout = 600f;  // 10분 내 처치 못하면 사라짐
        [SerializeField] private float announceBefore = 60f;   // 60초 전 예고

        [Header("보상")]
        [SerializeField] private int baseExpReward = 10000;
        [SerializeField] private int baseGoldReward = 5000;
        [SerializeField] private string[] guaranteedDrops;     // 확정 드롭 아이템ID
        [SerializeField] private string[] rareDrops;           // 랜덤 레어 드롭
        [SerializeField] private float rareDropChance = 0.1f;

        [Header("특수 공격")]
        [SerializeField] private float aoeRadius = 8f;
        [SerializeField] private float aoeDamageMultiplier = 2.5f;
        [SerializeField] private float enrageDamageMultiplier = 1.5f;
        [SerializeField] private float summonInterval = 30f;   // 쫄몹 소환 주기
        [SerializeField] private string summonMonsterId;        // 소환 몬스터 ID

        // Properties
        public string BossId => bossId;
        public string BossName => bossName;
        public string Description => description;
        public Sprite BossIcon => bossIcon;
        public long MaxHP => maxHP;
        public int Level => level;
        public float AttackDamage => attackDamage;
        public float Defense => defense;
        public float MagicDefense => magicDefense;
        public float Phase2Threshold => phase2Threshold;
        public float Phase3Threshold => phase3Threshold;
        public float EnrageThreshold => enrageThreshold;
        public float SpawnInterval => spawnInterval;
        public float DespawnTimeout => despawnTimeout;
        public float AnnounceBefore => announceBefore;
        public int BaseExpReward => baseExpReward;
        public int BaseGoldReward => baseGoldReward;
        public string[] GuaranteedDrops => guaranteedDrops;
        public string[] RareDrops => rareDrops;
        public float RareDropChance => rareDropChance;
        public float AoeRadius => aoeRadius;
        public float AoeDamageMultiplier => aoeDamageMultiplier;
        public float EnrageDamageMultiplier => enrageDamageMultiplier;
        public float SummonInterval => summonInterval;
        public string SummonMonsterId => summonMonsterId;
    }
}
