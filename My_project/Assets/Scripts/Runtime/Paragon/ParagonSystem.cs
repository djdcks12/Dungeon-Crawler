using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 파라곤 시스템 - 만렙 이후 무한 성장
    /// 5가지 카테고리 × 20노드 = 100개 노드
    /// 환생: 파라곤 리셋 → 영구 보너스 배율
    /// </summary>
    public class ParagonSystem : NetworkBehaviour
    {
        public static ParagonSystem Instance { get; private set; }

        [Header("파라곤 설정")]
        [SerializeField] private int maxPlayerLevel = 15;
        [SerializeField] private long expPerParagonPoint = 10000;
        [SerializeField] private float expScalePerPoint = 0.05f; // 포인트당 필요 경험치 5% 증가

        // 파라곤 카테고리
        public static readonly string[] CategoryNames = { "공격", "방어", "유틸리티", "스킬", "특수" };

        // 파라곤 노드 데이터 (카테고리별 20개)
        private readonly ParagonNode[][] nodeDatabase = new ParagonNode[][]
        {
            // 공격 카테고리 (20노드)
            new ParagonNode[]
            {
                new ParagonNode { id = "atk_str1", name = "힘 +1", statType = "STR", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "atk_str2", name = "힘 +2", statType = "STR", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "atk_dmg1", name = "데미지 +2%", statType = "DamagePercent", value = 2f, maxLevel = 10 },
                new ParagonNode { id = "atk_dmg2", name = "데미지 +5%", statType = "DamagePercent", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "atk_crit1", name = "크리율 +0.5%", statType = "CritRate", value = 0.5f, maxLevel = 10 },
                new ParagonNode { id = "atk_crit2", name = "크리뎀 +3%", statType = "CritDamage", value = 3f, maxLevel = 10 },
                new ParagonNode { id = "atk_pen", name = "방어 관통 +1", statType = "ArmorPen", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "atk_atkspd", name = "공속 +1%", statType = "AttackSpeed", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "atk_phys", name = "물리 마스터리 +2%", statType = "PhysMastery", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "atk_magic", name = "마법 마스터리 +2%", statType = "MagicMastery", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "atk_elem_fire", name = "화염 데미지 +3%", statType = "FireDmg", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "atk_elem_ice", name = "빙결 데미지 +3%", statType = "IceDmg", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "atk_elem_light", name = "번개 데미지 +3%", statType = "LightDmg", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "atk_lifesteal", name = "흡혈 +0.5%", statType = "Lifesteal", value = 0.5f, maxLevel = 5 },
                new ParagonNode { id = "atk_multistrike", name = "다중 타격 +2%", statType = "MultiStrike", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "atk_execute", name = "처형 +1%", statType = "Execute", value = 1f, maxLevel = 3, isKeystone = true },
                new ParagonNode { id = "atk_berserker", name = "광전사", statType = "Berserker", value = 1f, maxLevel = 1, isKeystone = true },
                new ParagonNode { id = "atk_agi1", name = "민첩 +1", statType = "AGI", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "atk_dot", name = "DoT 데미지 +3%", statType = "DotDmg", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "atk_boss", name = "보스 데미지 +5%", statType = "BossDmg", value = 5f, maxLevel = 5 },
            },
            // 방어 카테고리 (20노드)
            new ParagonNode[]
            {
                new ParagonNode { id = "def_vit1", name = "활력 +1", statType = "VIT", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "def_vit2", name = "활력 +2", statType = "VIT", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "def_def1", name = "방어 +2", statType = "DEF", value = 2f, maxLevel = 10 },
                new ParagonNode { id = "def_def2", name = "방어 +5", statType = "DEF", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "def_mdef1", name = "마방 +2", statType = "MDEF", value = 2f, maxLevel = 10 },
                new ParagonNode { id = "def_mdef2", name = "마방 +5", statType = "MDEF", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "def_hp1", name = "HP +50", statType = "FlatHP", value = 50f, maxLevel = 10 },
                new ParagonNode { id = "def_hp2", name = "HP +2%", statType = "HPPercent", value = 2f, maxLevel = 10 },
                new ParagonNode { id = "def_hpregen", name = "HP 재생 +0.5%", statType = "HPRegen", value = 0.5f, maxLevel = 5 },
                new ParagonNode { id = "def_block", name = "블록 +1%", statType = "Block", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "def_dodge", name = "회피 +0.5%", statType = "Dodge", value = 0.5f, maxLevel = 10 },
                new ParagonNode { id = "def_resist_fire", name = "화염 저항 +3%", statType = "FireRes", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "def_resist_ice", name = "빙결 저항 +3%", statType = "IceRes", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "def_resist_light", name = "번개 저항 +3%", statType = "LightRes", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "def_resist_poison", name = "독 저항 +3%", statType = "PoisonRes", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "def_cc_resist", name = "CC 저항 +2%", statType = "CCRes", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "def_thorns", name = "가시 +3", statType = "Thorns", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "def_stab", name = "안정성 +1", statType = "STAB", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "def_fortify", name = "요새화", statType = "Fortify", value = 1f, maxLevel = 1, isKeystone = true },
                new ParagonNode { id = "def_undying", name = "불사", statType = "Undying", value = 1f, maxLevel = 1, isKeystone = true },
            },
            // 유틸리티 카테고리 (20노드)
            new ParagonNode[]
            {
                new ParagonNode { id = "util_luk1", name = "행운 +1", statType = "LUK", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "util_luk2", name = "행운 +2", statType = "LUK", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "util_exp1", name = "경험치 +3%", statType = "ExpBonus", value = 3f, maxLevel = 10 },
                new ParagonNode { id = "util_gold1", name = "골드 +3%", statType = "GoldBonus", value = 3f, maxLevel = 10 },
                new ParagonNode { id = "util_drop1", name = "드롭률 +1%", statType = "DropRate", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "util_mf", name = "매직파인드 +2%", statType = "MagicFind", value = 2f, maxLevel = 10 },
                new ParagonNode { id = "util_movespd", name = "이동속도 +1%", statType = "MoveSpeed", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "util_pickup", name = "아이템 줍기 범위 +5%", statType = "PickupRange", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "util_gather", name = "채집 속도 +5%", statType = "GatherSpeed", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "util_craft", name = "제작 성공률 +2%", statType = "CraftBonus", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "util_enhance", name = "강화 성공률 +1%", statType = "EnhanceBonus", value = 1f, maxLevel = 5 },
                new ParagonNode { id = "util_shop", name = "상점 할인 +1%", statType = "ShopDiscount", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "util_inventory", name = "인벤토리 +1칸", statType = "InvSlot", value = 1f, maxLevel = 5 },
                new ParagonNode { id = "util_potion", name = "포션 효율 +5%", statType = "PotionEffect", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "util_xpboost", name = "파티 경험치 +2%", statType = "PartyExp", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "util_soul_rate", name = "소울 드롭률 +5%", statType = "SoulRate", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "util_expedition", name = "원정 시간 -3%", statType = "ExpedTime", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "util_rep_gain", name = "평판 획득 +5%", statType = "RepGain", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "util_treasure", name = "보물 사냥꾼", statType = "Treasure", value = 1f, maxLevel = 1, isKeystone = true },
                new ParagonNode { id = "util_lucky_star", name = "행운의 별", statType = "LuckyStar", value = 1f, maxLevel = 1, isKeystone = true },
            },
            // 스킬 카테고리 (20노드)
            new ParagonNode[]
            {
                new ParagonNode { id = "skill_int1", name = "지능 +1", statType = "INT", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "skill_int2", name = "지능 +2", statType = "INT", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "skill_cd1", name = "쿨다운 -1%", statType = "CDReduction", value = 1f, maxLevel = 10 },
                new ParagonNode { id = "skill_cd2", name = "쿨다운 -2%", statType = "CDReduction", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "skill_mana1", name = "마나 +20", statType = "FlatMP", value = 20f, maxLevel = 10 },
                new ParagonNode { id = "skill_mana2", name = "마나 +3%", statType = "MPPercent", value = 3f, maxLevel = 10 },
                new ParagonNode { id = "skill_mpregen", name = "마나 재생 +0.5%", statType = "MPRegen", value = 0.5f, maxLevel = 5 },
                new ParagonNode { id = "skill_manacost", name = "마나 소비 -2%", statType = "ManaCost", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "skill_aoe", name = "광역 범위 +3%", statType = "AoESize", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "skill_duration", name = "효과 지속시간 +3%", statType = "Duration", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "skill_proj", name = "투사체 속도 +5%", statType = "ProjSpeed", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "skill_buff", name = "버프 효과 +2%", statType = "BuffEffect", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "skill_debuff", name = "디버프 효과 +2%", statType = "DebuffEffect", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "skill_heal", name = "치유 효과 +3%", statType = "HealBonus", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "skill_shield", name = "보호막 +3%", statType = "ShieldBonus", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "skill_summon", name = "소환수 데미지 +5%", statType = "SummonDmg", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "skill_combo", name = "콤보 데미지 +5%", statType = "ComboDmg", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "skill_ultimate", name = "궁극기 쿨다운 -5%", statType = "UltCD", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "skill_arcane", name = "비전 마스터", statType = "Arcane", value = 1f, maxLevel = 1, isKeystone = true },
                new ParagonNode { id = "skill_echo", name = "스킬 메아리", statType = "SkillEcho", value = 1f, maxLevel = 1, isKeystone = true },
            },
            // 특수 카테고리 (20노드)
            new ParagonNode[]
            {
                new ParagonNode { id = "spec_all_stat", name = "전 스탯 +1", statType = "AllStat", value = 1f, maxLevel = 5 },
                new ParagonNode { id = "spec_all_res", name = "전 저항 +2%", statType = "AllRes", value = 2f, maxLevel = 5 },
                new ParagonNode { id = "spec_hybrid1", name = "STR+AGI +1", statType = "STR_AGI", value = 1f, maxLevel = 5 },
                new ParagonNode { id = "spec_hybrid2", name = "VIT+INT +1", statType = "VIT_INT", value = 1f, maxLevel = 5 },
                new ParagonNode { id = "spec_nightmare", name = "악몽 던전 보상 +5%", statType = "NightmareBonus", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "spec_bossrush", name = "보스 러쉬 데미지 +5%", statType = "RushDmg", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "spec_pvp", name = "PvP 데미지 +3%", statType = "PvPDmg", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "spec_pet", name = "펫 효과 +5%", statType = "PetBonus", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "spec_housing", name = "하우징 버프 +10%", statType = "HousingBonus", value = 10f, maxLevel = 3 },
                new ParagonNode { id = "spec_party", name = "파티 시너지 +3%", statType = "PartySynergy", value = 3f, maxLevel = 5 },
                new ParagonNode { id = "spec_guild", name = "길드 버프 +5%", statType = "GuildBonus", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "spec_awakening", name = "각성 효과 +5%", statType = "AwakeBonus", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "spec_rune", name = "룬 효과 +5%", statType = "RuneBonus", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "spec_set", name = "세트 효과 +5%", statType = "SetBonus", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "spec_season", name = "시즌 경험치 +10%", statType = "SeasonExp", value = 10f, maxLevel = 3 },
                new ParagonNode { id = "spec_merc", name = "용병 효과 +5%", statType = "MercBonus", value = 5f, maxLevel = 5 },
                new ParagonNode { id = "spec_reputation", name = "전 진영 평판 +5%", statType = "AllRepBonus", value = 5f, maxLevel = 3 },
                new ParagonNode { id = "spec_rebirth_exp", name = "환생 경험치 +10%", statType = "RebirthExp", value = 10f, maxLevel = 5 },
                new ParagonNode { id = "spec_transcend", name = "초월", statType = "Transcend", value = 1f, maxLevel = 1, isKeystone = true },
                new ParagonNode { id = "spec_infinity", name = "무한의 힘", statType = "Infinity", value = 1f, maxLevel = 1, isKeystone = true },
            }
        };

        // 서버: 플레이어별 파라곤 데이터
        private Dictionary<ulong, ParagonPlayerData> playerParagon = new Dictionary<ulong, ParagonPlayerData>();

        // 로컬
        private ParagonPlayerData localParagon;

        // 이벤트
        public System.Action<int> OnParagonPointGained; // total points
        public System.Action<string, int> OnNodeAllocated; // nodeId, newLevel
        public System.Action<int> OnRebirth; // rebirth count

        // 접근자
        public ParagonPlayerData LocalParagon => localParagon;
        public int TotalPoints => localParagon?.totalPoints ?? 0;
        public int SpentPoints => localParagon?.spentPoints ?? 0;
        public int AvailablePoints => TotalPoints - SpentPoints;
        public int RebirthCount => localParagon?.rebirthCount ?? 0;
        public float RebirthMultiplier => 1f + (localParagon?.rebirthCount ?? 0) * 0.05f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadLocalParagon();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                PlayerStatsData.OnLevelUp += OnPlayerLevelUp;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                PlayerStatsData.OnLevelUp -= OnPlayerLevelUp;
            base.OnNetworkDespawn();
        }

        private void OnPlayerLevelUp(int newLevel)
        {
            if (newLevel == maxPlayerLevel)
            {
                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification("<color=#FFD700>만렙 달성!</color> 파라곤 시스템이 활성화되었습니다.", NotificationType.Achievement);
            }
        }

        #region 파라곤 포인트

        /// <summary>
        /// 파라곤 경험치 추가 (서버)
        /// </summary>
        public void AddParagonExp(ulong clientId, long exp)
        {
            if (!IsServer) return;

            if (!playerParagon.ContainsKey(clientId))
                playerParagon[clientId] = new ParagonPlayerData();

            var data = playerParagon[clientId];
            float rebirthMult = 1f + data.rebirthCount * 0.05f;
            data.currentExp += (long)(exp * rebirthMult);

            long needed = GetExpForNextPoint(data.totalPoints);
            while (data.currentExp >= needed)
            {
                data.currentExp -= needed;
                data.totalPoints++;
                needed = GetExpForNextPoint(data.totalPoints);
            }

            NotifyParagonUpdateClientRpc(data.totalPoints, data.spentPoints, data.currentExp, clientId);
        }

        /// <summary>
        /// 다음 포인트 필요 경험치
        /// </summary>
        public long GetExpForNextPoint(int currentPoints)
        {
            return (long)(expPerParagonPoint * (1f + expScalePerPoint * currentPoints));
        }

        #endregion

        #region 노드 할당

        /// <summary>
        /// 노드 정보 조회
        /// </summary>
        public ParagonNode GetNode(int category, int nodeIndex)
        {
            if (category < 0 || category >= nodeDatabase.Length) return null;
            if (nodeIndex < 0 || nodeIndex >= nodeDatabase[category].Length) return null;
            return nodeDatabase[category][nodeIndex];
        }

        /// <summary>
        /// 노드 현재 레벨
        /// </summary>
        public int GetNodeLevel(string nodeId)
        {
            if (localParagon == null) return 0;
            if (localParagon.allocatedNodes.TryGetValue(nodeId, out int level))
                return level;
            return 0;
        }

        /// <summary>
        /// 카테고리별 노드 목록
        /// </summary>
        public ParagonNode[] GetCategoryNodes(int category)
        {
            if (category < 0 || category >= nodeDatabase.Length) return new ParagonNode[0];
            return nodeDatabase[category];
        }

        /// <summary>
        /// 노드 할당
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AllocateNodeServerRpc(string nodeId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerParagon.ContainsKey(clientId))
                playerParagon[clientId] = new ParagonPlayerData();

            var data = playerParagon[clientId];

            // 포인트 확인
            if (data.totalPoints <= data.spentPoints)
            {
                SendMessageClientRpc("파라곤 포인트가 부족합니다.", clientId);
                return;
            }

            // 노드 검색
            ParagonNode targetNode = null;
            foreach (var category in nodeDatabase)
            {
                foreach (var node in category)
                {
                    if (node.id == nodeId) { targetNode = node; break; }
                }
                if (targetNode != null) break;
            }

            if (targetNode == null)
            {
                SendMessageClientRpc("존재하지 않는 노드입니다.", clientId);
                return;
            }

            // 레벨 확인
            int currentLevel = 0;
            if (data.allocatedNodes.ContainsKey(nodeId))
                currentLevel = data.allocatedNodes[nodeId];

            if (currentLevel >= targetNode.maxLevel)
            {
                SendMessageClientRpc("이미 최대 레벨입니다.", clientId);
                return;
            }

            // 할당
            data.allocatedNodes[nodeId] = currentLevel + 1;
            data.spentPoints++;

            NotifyNodeAllocatedClientRpc(nodeId, currentLevel + 1, data.totalPoints, data.spentPoints, clientId);
        }

        #endregion

        #region 환생

        /// <summary>
        /// 환생: 파라곤 초기화 → 영구 보너스 배율
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RebirthServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!playerParagon.ContainsKey(clientId))
            {
                SendMessageClientRpc("파라곤 데이터가 없습니다.", clientId);
                return;
            }

            var data = playerParagon[clientId];

            // 최소 50포인트 투자 필요
            if (data.spentPoints < 50)
            {
                SendMessageClientRpc("환생하려면 최소 50 파라곤 포인트를 투자해야 합니다.", clientId);
                return;
            }

            // 환생
            data.rebirthCount++;
            data.totalPoints = 0;
            data.spentPoints = 0;
            data.currentExp = 0;
            data.allocatedNodes.Clear();

            NotifyRebirthClientRpc(data.rebirthCount, clientId);
        }

        #endregion

        #region 보너스 계산

        /// <summary>
        /// 특정 스탯 타입의 총 파라곤 보너스
        /// </summary>
        public float GetStatBonus(string statType)
        {
            if (localParagon == null) return 0f;

            float total = 0f;
            foreach (var category in nodeDatabase)
            {
                foreach (var node in category)
                {
                    if (node.statType == statType && localParagon.allocatedNodes.TryGetValue(node.id, out int level))
                    {
                        total += node.value * level;
                    }
                }
            }

            return total * RebirthMultiplier;
        }

        /// <summary>
        /// 키스톤 활성 여부
        /// </summary>
        public bool HasKeystone(string nodeId)
        {
            if (localParagon == null) return false;
            return localParagon.allocatedNodes.ContainsKey(nodeId) && localParagon.allocatedNodes[nodeId] > 0;
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyParagonUpdateClientRpc(int totalPts, int spentPts, long currentExp, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localParagon == null) localParagon = new ParagonPlayerData();
            localParagon.totalPoints = totalPts;
            localParagon.spentPoints = spentPts;
            localParagon.currentExp = currentExp;
            OnParagonPointGained?.Invoke(totalPts);
            SaveLocalParagon();
        }

        [ClientRpc]
        private void NotifyNodeAllocatedClientRpc(string nodeId, int newLevel, int totalPts, int spentPts, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localParagon == null) localParagon = new ParagonPlayerData();
            localParagon.allocatedNodes[nodeId] = newLevel;
            localParagon.totalPoints = totalPts;
            localParagon.spentPoints = spentPts;
            OnNodeAllocated?.Invoke(nodeId, newLevel);
            SaveLocalParagon();
        }

        [ClientRpc]
        private void NotifyRebirthClientRpc(int rebirthCount, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            if (localParagon == null) localParagon = new ParagonPlayerData();
            localParagon.rebirthCount = rebirthCount;
            localParagon.totalPoints = 0;
            localParagon.spentPoints = 0;
            localParagon.currentExp = 0;
            localParagon.allocatedNodes.Clear();
            OnRebirth?.Invoke(rebirthCount);
            SaveLocalParagon();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"<color=#FFD700>환생 {rebirthCount}회!</color> 영구 보너스 +{rebirthCount * 5}%", NotificationType.Achievement);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region 로컬 저장

        private void SaveLocalParagon()
        {
            if (localParagon == null) return;
            var save = new ParagonSaveData
            {
                totalPoints = localParagon.totalPoints,
                spentPoints = localParagon.spentPoints,
                currentExp = localParagon.currentExp,
                rebirthCount = localParagon.rebirthCount,
                nodeIds = new List<string>(localParagon.allocatedNodes.Keys).ToArray(),
                nodeLevels = new List<int>(localParagon.allocatedNodes.Values).ToArray()
            };
            PlayerPrefs.SetString("Paragon_Data", JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void LoadLocalParagon()
        {
            localParagon = new ParagonPlayerData();
            string json = PlayerPrefs.GetString("Paragon_Data", "");
            if (!string.IsNullOrEmpty(json))
            {
                var save = JsonUtility.FromJson<ParagonSaveData>(json);
                if (save != null)
                {
                    localParagon.totalPoints = save.totalPoints;
                    localParagon.spentPoints = save.spentPoints;
                    localParagon.currentExp = save.currentExp;
                    localParagon.rebirthCount = save.rebirthCount;
                    if (save.nodeIds != null)
                    {
                        for (int i = 0; i < save.nodeIds.Length && i < save.nodeLevels.Length; i++)
                            localParagon.allocatedNodes[save.nodeIds[i]] = save.nodeLevels[i];
                    }
                }
            }
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }

    #region 데이터 구조체

    [System.Serializable]
    public class ParagonNode
    {
        public string id;
        public string name;
        public string statType;
        public float value;
        public int maxLevel;
        public bool isKeystone;
    }

    public class ParagonPlayerData
    {
        public int totalPoints;
        public int spentPoints;
        public long currentExp;
        public int rebirthCount;
        public Dictionary<string, int> allocatedNodes = new Dictionary<string, int>();
    }

    [System.Serializable]
    public class ParagonSaveData
    {
        public int totalPoints;
        public int spentPoints;
        public long currentExp;
        public int rebirthCount;
        public string[] nodeIds;
        public int[] nodeLevels;
    }

    #endregion
}
