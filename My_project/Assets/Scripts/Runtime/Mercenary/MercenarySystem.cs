using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 용병/동료 시스템 - 솔로 플레이 강화
    /// 6종 용병, 장비 슬롯, 스킬 3개, 레벨/충성도/특성
    /// AI 행동 설정 (공격적/방어적/지원)
    /// </summary>
    public class MercenarySystem : NetworkBehaviour
    {
        public static MercenarySystem Instance { get; private set; }

        [Header("용병 설정")]
        [SerializeField] private int maxMercenaryLevel = 30;
        [SerializeField] private int maxLoyalty = 100;
        [SerializeField] private float loyaltyGainPerMinute = 1f;
        [SerializeField] private int traitUnlockInterval = 5; // 5레벨마다 특성 해금

        // 용병 데이터베이스
        private readonly MercenaryTemplate[] mercenaryTemplates = new MercenaryTemplate[]
        {
            new MercenaryTemplate
            {
                id = "warrior", name = "철벽의 가렉", type = MercenaryType.Warrior,
                description = "강인한 전사. 전방에서 적의 공격을 막아줍니다.",
                baseStat = new MercStat { hp = 500, atk = 25, def = 20, mdef = 8, spd = 5 },
                growthStat = new MercStat { hp = 50, atk = 3, def = 2.5f, mdef = 1, spd = 0.3f },
                hireCost = 10000, dailyCost = 500,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "도발", desc = "적 3체를 3초간 도발", cooldown = 12f, damagePercent = 0 },
                    new MercSkillInfo { name = "방패 강타", desc = "전방 적에게 공격력 150% 데미지 + 1초 기절", cooldown = 8f, damagePercent = 1.5f },
                    new MercSkillInfo { name = "전투 함성", desc = "주변 아군 공격력 +15%, 8초", cooldown = 25f, damagePercent = 0 }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "강화 방패", desc = "방어력 +10%", requiredLevel = 5, statType = "DEF", value = 0.10f },
                    new MercTrait { name = "불굴", desc = "HP 20% 이하 시 방어력 2배", requiredLevel = 10, statType = "UNDYING", value = 2f },
                    new MercTrait { name = "보호 본능", desc = "플레이어가 받는 데미지 15% 대신 받기", requiredLevel = 15, statType = "PROTECT", value = 0.15f },
                    new MercTrait { name = "강철 의지", desc = "HP +20%", requiredLevel = 20, statType = "HP", value = 0.20f },
                    new MercTrait { name = "불멸 수호자", desc = "사망 시 1회 부활 (1분 쿨다운)", requiredLevel = 25, statType = "REVIVE", value = 1f },
                    new MercTrait { name = "전쟁 군주", desc = "전 스탯 +10%", requiredLevel = 30, statType = "ALL", value = 0.10f }
                }
            },
            new MercenaryTemplate
            {
                id = "archer", name = "바람의 셀린", type = MercenaryType.Archer,
                description = "뛰어난 궁수. 먼 거리에서 정확한 사격을 합니다.",
                baseStat = new MercStat { hp = 300, atk = 35, def = 8, mdef = 10, spd = 12 },
                growthStat = new MercStat { hp = 30, atk = 4, def = 0.8f, mdef = 1, spd = 0.5f },
                hireCost = 12000, dailyCost = 600,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "다발 사격", desc = "전방 3체에게 공격력 80% 데미지", cooldown = 6f, damagePercent = 0.8f },
                    new MercSkillInfo { name = "급소 사격", desc = "단일 대상 공격력 250% 데미지, 크리율 +30%", cooldown = 10f, damagePercent = 2.5f },
                    new MercSkillInfo { name = "화살비", desc = "범위 내 적에게 공격력 60% 데미지 × 5회", cooldown = 20f, damagePercent = 0.6f }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "예리한 눈", desc = "크리율 +8%", requiredLevel = 5, statType = "CRIT", value = 8f },
                    new MercTrait { name = "빠른 장전", desc = "공격속도 +15%", requiredLevel = 10, statType = "ASPD", value = 0.15f },
                    new MercTrait { name = "관통 화살", desc = "방어 관통 +20%", requiredLevel = 15, statType = "PEN", value = 0.20f },
                    new MercTrait { name = "치명적 정밀", desc = "크리 데미지 +25%", requiredLevel = 20, statType = "CRITDMG", value = 25f },
                    new MercTrait { name = "죽음의 표식", desc = "공격 대상에게 표식 → 10% 추가 데미지", requiredLevel = 25, statType = "MARK", value = 0.10f },
                    new MercTrait { name = "명사수", desc = "공격력 +20%", requiredLevel = 30, statType = "ATK", value = 0.20f }
                }
            },
            new MercenaryTemplate
            {
                id = "mage", name = "마도사 엘드린", type = MercenaryType.Mage,
                description = "강력한 마법사. 광역 마법으로 다수의 적을 처리합니다.",
                baseStat = new MercStat { hp = 250, atk = 40, def = 5, mdef = 15, spd = 6 },
                growthStat = new MercStat { hp = 25, atk = 5, def = 0.5f, mdef = 2, spd = 0.3f },
                hireCost = 15000, dailyCost = 750,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "파이어볼", desc = "범위 공격, 공격력 200% 화염 데미지", cooldown = 8f, damagePercent = 2.0f },
                    new MercSkillInfo { name = "아이스 스톰", desc = "범위 내 적 공격력 120% + 둔화 50%, 4초", cooldown = 15f, damagePercent = 1.2f },
                    new MercSkillInfo { name = "메테오", desc = "대범위 공격력 350% 화염 데미지", cooldown = 30f, damagePercent = 3.5f }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "마나 친화", desc = "스킬 쿨다운 -10%", requiredLevel = 5, statType = "CDR", value = 0.10f },
                    new MercTrait { name = "원소 강화", desc = "마법 데미지 +15%", requiredLevel = 10, statType = "MDMG", value = 0.15f },
                    new MercTrait { name = "마력 폭발", desc = "킬 시 10% 확률 광역 폭발", requiredLevel = 15, statType = "EXPLOSION", value = 0.10f },
                    new MercTrait { name = "마법 방벽", desc = "마방 +30%", requiredLevel = 20, statType = "MDEF", value = 0.30f },
                    new MercTrait { name = "시간 왜곡", desc = "주변 적 이동속도 -20%", requiredLevel = 25, statType = "SLOW", value = 0.20f },
                    new MercTrait { name = "대마도사", desc = "전 스킬 데미지 +25%", requiredLevel = 30, statType = "SKILLDMG", value = 0.25f }
                }
            },
            new MercenaryTemplate
            {
                id = "healer", name = "성녀 리아나", type = MercenaryType.Healer,
                description = "뛰어난 치유사. 플레이어의 HP를 회복시켜줍니다.",
                baseStat = new MercStat { hp = 350, atk = 15, def = 10, mdef = 18, spd = 7 },
                growthStat = new MercStat { hp = 35, atk = 1.5f, def = 1, mdef = 2.5f, spd = 0.3f },
                hireCost = 20000, dailyCost = 1000,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "치유의 빛", desc = "플레이어 HP 최대HP의 20% 회복", cooldown = 10f, damagePercent = 0 },
                    new MercSkillInfo { name = "보호의 기도", desc = "플레이어에게 보호막 (최대HP 15%), 8초", cooldown = 18f, damagePercent = 0 },
                    new MercSkillInfo { name = "부활의 은총", desc = "플레이어 사망 시 자동 부활 (HP 50%)", cooldown = 60f, damagePercent = 0 }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "강화 치유", desc = "치유량 +15%", requiredLevel = 5, statType = "HEAL", value = 0.15f },
                    new MercTrait { name = "정화", desc = "치유 시 디버프 1개 제거", requiredLevel = 10, statType = "CLEANSE", value = 1f },
                    new MercTrait { name = "재생 오라", desc = "주변 아군 HP 재생 +1%/초", requiredLevel = 15, statType = "REGEN", value = 0.01f },
                    new MercTrait { name = "성스러운 방벽", desc = "보호막 지속시간 +50%", requiredLevel = 20, statType = "SHIELDDUR", value = 0.50f },
                    new MercTrait { name = "축복", desc = "플레이어 경험치 획득 +10%", requiredLevel = 25, statType = "EXP", value = 0.10f },
                    new MercTrait { name = "대성녀", desc = "모든 치유 효과 +30%, 스킬 쿨다운 -20%", requiredLevel = 30, statType = "HEALALL", value = 0.30f }
                }
            },
            new MercenaryTemplate
            {
                id = "assassin", name = "그림자 카인", type = MercenaryType.Assassin,
                description = "은밀한 암살자. 높은 크리티컬과 단일 대상 폭딜.",
                baseStat = new MercStat { hp = 280, atk = 38, def = 6, mdef = 6, spd = 15 },
                growthStat = new MercStat { hp = 28, atk = 4.5f, def = 0.6f, mdef = 0.6f, spd = 0.8f },
                hireCost = 18000, dailyCost = 900,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "급습", desc = "적 뒤로 순간이동, 공격력 200% + 100% 크리", cooldown = 8f, damagePercent = 2.0f },
                    new MercSkillInfo { name = "독날", desc = "대상에게 독 DoT (공격력 30% × 5초)", cooldown = 10f, damagePercent = 0.3f },
                    new MercSkillInfo { name = "처형", desc = "HP 30% 이하 적에게 공격력 500% 데미지", cooldown = 15f, damagePercent = 5.0f }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "그림자 일격", desc = "크리율 +10%", requiredLevel = 5, statType = "CRIT", value = 10f },
                    new MercTrait { name = "맹독", desc = "독 데미지 +50%", requiredLevel = 10, statType = "POISON", value = 0.50f },
                    new MercTrait { name = "은밀", desc = "전투 시작 시 3초간 은신 (무적)", requiredLevel = 15, statType = "STEALTH", value = 3f },
                    new MercTrait { name = "급소 공략", desc = "크리 데미지 +40%", requiredLevel = 20, statType = "CRITDMG", value = 40f },
                    new MercTrait { name = "연쇄 처형", desc = "킬 시 다음 공격 데미지 +50%", requiredLevel = 25, statType = "CHAIN", value = 0.50f },
                    new MercTrait { name = "죽음의 춤", desc = "회피 +15%, 공격력 +15%", requiredLevel = 30, statType = "ALL", value = 0.15f }
                }
            },
            new MercenaryTemplate
            {
                id = "guardian", name = "수호자 토르겐", type = MercenaryType.Guardian,
                description = "성스러운 수호자. 공격과 방어의 균형을 갖춘 만능형.",
                baseStat = new MercStat { hp = 450, atk = 22, def = 18, mdef = 14, spd = 6 },
                growthStat = new MercStat { hp = 45, atk = 2.5f, def = 2, mdef = 1.5f, spd = 0.3f },
                hireCost = 25000, dailyCost = 1200,
                skills = new MercSkillInfo[]
                {
                    new MercSkillInfo { name = "심판의 일격", desc = "전방 적에게 공격력 180% 성스러운 데미지", cooldown = 7f, damagePercent = 1.8f },
                    new MercSkillInfo { name = "수호의 서약", desc = "자신+플레이어 방어력 +25%, 10초", cooldown = 20f, damagePercent = 0 },
                    new MercSkillInfo { name = "천벌", desc = "주변 전체에 공격력 250% 성스러운 데미지 + 언데드 2배", cooldown = 25f, damagePercent = 2.5f }
                },
                traits = new MercTrait[]
                {
                    new MercTrait { name = "축복받은 갑옷", desc = "방어력 +15%, 마방 +15%", requiredLevel = 5, statType = "ALLDEF", value = 0.15f },
                    new MercTrait { name = "성스러운 검", desc = "언데드/악마에게 데미지 +30%", requiredLevel = 10, statType = "HOLY", value = 0.30f },
                    new MercTrait { name = "보복", desc = "피격 시 15% 확률로 반격 (공격력 100%)", requiredLevel = 15, statType = "COUNTER", value = 0.15f },
                    new MercTrait { name = "생명력 강화", desc = "HP +25%", requiredLevel = 20, statType = "HP", value = 0.25f },
                    new MercTrait { name = "수호 오라", desc = "주변 아군 방어력 +10%", requiredLevel = 25, statType = "AURA_DEF", value = 0.10f },
                    new MercTrait { name = "불멸의 수호자", desc = "HP 10% 이하에서 5초간 무적 (5분 쿨타임)", requiredLevel = 30, statType = "INVULN", value = 5f }
                }
            }
        };

        // 서버: 플레이어별 활성 용병
        private Dictionary<ulong, ActiveMercenary> activeMercenaries = new Dictionary<ulong, ActiveMercenary>();

        // 로컬: 보유 용병 데이터
        private Dictionary<string, MercenarySaveData> ownedMercenaries = new Dictionary<string, MercenarySaveData>();
        private string localActiveMercId;

        // 이벤트
        public System.Action<string> OnMercenaryHired; // mercId
        public System.Action<string, int> OnMercenaryLevelUp; // mercId, newLevel
        public System.Action<string> OnMercenaryActivated; // mercId
        public System.Action OnMercenaryDismissed;
        public System.Action<string, MercBehavior> OnBehaviorChanged;

        // 접근자
        public string ActiveMercenaryId => localActiveMercId;
        public bool HasActiveMercenary => !string.IsNullOrEmpty(localActiveMercId);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadMercenaryData();
        }

        #region 용병 정보

        /// <summary>
        /// 용병 템플릿 조회
        /// </summary>
        public MercenaryTemplate GetTemplate(string mercId)
        {
            foreach (var t in mercenaryTemplates)
                if (t.id == mercId) return t;
            return null;
        }

        /// <summary>
        /// 전체 용병 목록
        /// </summary>
        public MercenaryTemplate[] GetAllTemplates() => mercenaryTemplates;

        /// <summary>
        /// 보유 용병인지 확인
        /// </summary>
        public bool IsOwned(string mercId) => ownedMercenaries.ContainsKey(mercId);

        /// <summary>
        /// 용병 세이브 데이터 조회
        /// </summary>
        public MercenarySaveData GetSaveData(string mercId)
        {
            ownedMercenaries.TryGetValue(mercId, out var data);
            return data;
        }

        /// <summary>
        /// 보유 용병 전체 목록
        /// </summary>
        public Dictionary<string, MercenarySaveData> GetAllOwned() => ownedMercenaries;

        /// <summary>
        /// 활성 용병의 현재 스탯 계산
        /// </summary>
        public MercStat GetCurrentStats(string mercId)
        {
            var template = GetTemplate(mercId);
            if (template == null) return default;

            var save = GetSaveData(mercId);
            int level = save?.level ?? 1;

            return new MercStat
            {
                hp = template.baseStat.hp + template.growthStat.hp * (level - 1),
                atk = template.baseStat.atk + template.growthStat.atk * (level - 1),
                def = template.baseStat.def + template.growthStat.def * (level - 1),
                mdef = template.baseStat.mdef + template.growthStat.mdef * (level - 1),
                spd = template.baseStat.spd + template.growthStat.spd * (level - 1)
            };
        }

        /// <summary>
        /// 해금된 특성 목록
        /// </summary>
        public List<MercTrait> GetUnlockedTraits(string mercId)
        {
            var template = GetTemplate(mercId);
            var save = GetSaveData(mercId);
            if (template == null || save == null) return new List<MercTrait>();

            var result = new List<MercTrait>();
            foreach (var trait in template.traits)
            {
                if (save.level >= trait.requiredLevel)
                    result.Add(trait);
            }
            return result;
        }

        /// <summary>
        /// 다음 레벨업 필요 경험치
        /// </summary>
        public long GetExpForNextLevel(int currentLevel)
        {
            return 1000L + (long)(currentLevel * currentLevel * 100);
        }

        /// <summary>
        /// 충성도 등급
        /// </summary>
        public string GetLoyaltyRank(int loyalty)
        {
            if (loyalty >= 90) return "절대적 충성";
            if (loyalty >= 70) return "깊은 신뢰";
            if (loyalty >= 50) return "신뢰";
            if (loyalty >= 30) return "호감";
            if (loyalty >= 10) return "보통";
            return "경계";
        }

        /// <summary>
        /// 충성도에 따른 전투력 보너스
        /// </summary>
        public float GetLoyaltyBonus(int loyalty)
        {
            if (loyalty >= 90) return 0.20f;
            if (loyalty >= 70) return 0.15f;
            if (loyalty >= 50) return 0.10f;
            if (loyalty >= 30) return 0.05f;
            return 0;
        }

        #endregion

        #region 용병 고용/해고

        /// <summary>
        /// 용병 고용
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void HireMercenaryServerRpc(string mercId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            var template = GetTemplate(mercId);
            if (template == null)
            {
                SendMessageClientRpc("존재하지 않는 용병입니다.", clientId);
                return;
            }

            // 비용 확인
            if (statsData.Gold < template.hireCost)
            {
                SendMessageClientRpc($"골드 부족 (필요: {template.hireCost:N0}G)", clientId);
                return;
            }

            statsData.ChangeGold(-template.hireCost);
            NotifyMercenaryHiredClientRpc(clientId, mercId);
        }

        /// <summary>
        /// 용병 활성화 (동행)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ActivateMercenaryServerRpc(string mercId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var template = GetTemplate(mercId);
            if (template == null) return;

            activeMercenaries[clientId] = new ActiveMercenary
            {
                mercId = mercId,
                currentHP = GetCurrentStats(mercId).hp,
                behavior = MercBehavior.Balanced,
                skillCooldowns = new float[3]
            };

            NotifyMercenaryActivatedClientRpc(clientId, mercId);
        }

        /// <summary>
        /// 용병 비활성화
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DismissMercenaryServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            activeMercenaries.Remove(clientId);
            NotifyMercenaryDismissedClientRpc(clientId);
        }

        /// <summary>
        /// AI 행동 모드 변경
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetBehaviorServerRpc(int behaviorInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!activeMercenaries.ContainsKey(clientId)) return;

            var behavior = (MercBehavior)behaviorInt;
            activeMercenaries[clientId].behavior = behavior;
            NotifyBehaviorChangedClientRpc(clientId, activeMercenaries[clientId].mercId, behaviorInt);
        }

        #endregion

        #region 전투/경험치

        /// <summary>
        /// 용병 경험치 추가 (전투 참여)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddMercenaryExpServerRpc(long exp, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!activeMercenaries.ContainsKey(clientId)) return;

            string mercId = activeMercenaries[clientId].mercId;
            NotifyMercenaryExpClientRpc(clientId, mercId, exp);
        }

        /// <summary>
        /// 용병 충성도 증가 (동행 시간)
        /// </summary>
        private void Update()
        {
            if (!IsServer) return;

            // 매 분마다 충성도 증가
            if (Time.frameCount % (60 * 60) == 0) // ~60fps * 60초
            {
                foreach (var kvp in activeMercenaries)
                {
                    NotifyLoyaltyGainClientRpc(kvp.Key, kvp.Value.mercId, Mathf.RoundToInt(loyaltyGainPerMinute));
                }
            }
        }

        /// <summary>
        /// 용병 데미지 계산 (서버)
        /// </summary>
        public float CalculateMercenaryDamage(ulong clientId)
        {
            if (!activeMercenaries.TryGetValue(clientId, out var merc)) return 0;

            var stats = GetCurrentStats(merc.mercId);
            var save = GetSaveData(merc.mercId);
            float loyaltyBonus = save != null ? GetLoyaltyBonus(save.loyalty) : 0;

            // 행동 모드 보정
            float behaviorMult = merc.behavior switch
            {
                MercBehavior.Aggressive => 1.3f,
                MercBehavior.Defensive => 0.7f,
                MercBehavior.Support => 0.5f,
                _ => 1.0f
            };

            return stats.atk * (1f + loyaltyBonus) * behaviorMult;
        }

        /// <summary>
        /// 용병이 줄 수 있는 플레이어 버프 요약
        /// </summary>
        public Dictionary<string, float> GetMercenaryBuffs(string mercId)
        {
            var buffs = new Dictionary<string, float>();
            var traits = GetUnlockedTraits(mercId);

            foreach (var trait in traits)
            {
                if (buffs.ContainsKey(trait.statType))
                    buffs[trait.statType] += trait.value;
                else
                    buffs[trait.statType] = trait.value;
            }

            return buffs;
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyMercenaryHiredClientRpc(ulong targetClientId, string mercId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            // 로컬 데이터 생성
            if (!ownedMercenaries.ContainsKey(mercId))
            {
                ownedMercenaries[mercId] = new MercenarySaveData
                {
                    mercId = mercId,
                    level = 1,
                    exp = 0,
                    loyalty = 10,
                    behavior = MercBehavior.Balanced
                };
                SaveMercenaryData();
            }

            OnMercenaryHired?.Invoke(mercId);

            var template = GetTemplate(mercId);
            var notif = NotificationManager.Instance;
            if (notif != null && template != null)
                notif.ShowNotification($"<color=#FFD700>{template.name}</color> 고용 완료!", NotificationType.Achievement);
        }

        [ClientRpc]
        private void NotifyMercenaryActivatedClientRpc(ulong targetClientId, string mercId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localActiveMercId = mercId;
            if (ownedMercenaries.ContainsKey(mercId))
            {
                ownedMercenaries[mercId].behavior = MercBehavior.Balanced;
                SaveMercenaryData();
            }

            OnMercenaryActivated?.Invoke(mercId);

            var template = GetTemplate(mercId);
            var notif = NotificationManager.Instance;
            if (notif != null && template != null)
                notif.ShowNotification($"{template.name}이(가) 동행을 시작합니다.", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMercenaryDismissedClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            localActiveMercId = null;
            OnMercenaryDismissed?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("용병을 해산했습니다.", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBehaviorChangedClientRpc(ulong targetClientId, string mercId, int behaviorInt)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var behavior = (MercBehavior)behaviorInt;
            if (ownedMercenaries.ContainsKey(mercId))
            {
                ownedMercenaries[mercId].behavior = behavior;
                SaveMercenaryData();
            }

            OnBehaviorChanged?.Invoke(mercId, behavior);

            string behaviorName = behavior switch
            {
                MercBehavior.Aggressive => "공격적",
                MercBehavior.Defensive => "방어적",
                MercBehavior.Support => "지원",
                _ => "균형"
            };

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"용병 행동: {behaviorName}", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyMercenaryExpClientRpc(ulong targetClientId, string mercId, long exp)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (!ownedMercenaries.ContainsKey(mercId)) return;
            var save = ownedMercenaries[mercId];
            save.exp += exp;

            // 레벨업 체크
            while (save.level < maxMercenaryLevel)
            {
                long needed = GetExpForNextLevel(save.level);
                if (save.exp < needed) break;

                save.exp -= needed;
                save.level++;

                OnMercenaryLevelUp?.Invoke(mercId, save.level);

                var template = GetTemplate(mercId);
                var notif = NotificationManager.Instance;
                if (notif != null && template != null)
                {
                    notif.ShowNotification($"{template.name} 레벨 {save.level} 달성!", NotificationType.System);

                    // 특성 해금 알림
                    foreach (var trait in template.traits)
                    {
                        if (trait.requiredLevel == save.level)
                            notif.ShowNotification($"<color=#FF8800>특성 해금: {trait.name}</color> - {trait.desc}", NotificationType.Achievement);
                    }
                }
            }

            SaveMercenaryData();
        }

        [ClientRpc]
        private void NotifyLoyaltyGainClientRpc(ulong targetClientId, string mercId, int amount)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (!ownedMercenaries.ContainsKey(mercId)) return;
            var save = ownedMercenaries[mercId];
            int oldLoyalty = save.loyalty;
            save.loyalty = Mathf.Min(save.loyalty + amount, maxLoyalty);

            // 등급 변화 알림
            string oldRank = GetLoyaltyRank(oldLoyalty);
            string newRank = GetLoyaltyRank(save.loyalty);
            if (oldRank != newRank)
            {
                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification($"용병 충성도: {newRank}", NotificationType.System);
            }

            SaveMercenaryData();
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

        #region 저장/로드

        private void SaveMercenaryData()
        {
            var ids = new List<string>(ownedMercenaries.Keys);
            PlayerPrefs.SetString("Merc_Owned", string.Join(",", ids));
            PlayerPrefs.SetString("Merc_Active", localActiveMercId ?? "");

            foreach (var kvp in ownedMercenaries)
            {
                string prefix = $"Merc_{kvp.Key}_";
                PlayerPrefs.SetInt(prefix + "Level", kvp.Value.level);
                PlayerPrefs.SetString(prefix + "Exp", kvp.Value.exp.ToString());
                PlayerPrefs.SetInt(prefix + "Loyalty", kvp.Value.loyalty);
                PlayerPrefs.SetInt(prefix + "Behavior", (int)kvp.Value.behavior);
                PlayerPrefs.SetString(prefix + "Weapon", kvp.Value.equippedWeapon ?? "");
                PlayerPrefs.SetString(prefix + "Armor", kvp.Value.equippedArmor ?? "");
            }
            PlayerPrefs.Save();
        }

        private void LoadMercenaryData()
        {
            string ownedStr = PlayerPrefs.GetString("Merc_Owned", "");
            localActiveMercId = PlayerPrefs.GetString("Merc_Active", "");
            if (string.IsNullOrEmpty(localActiveMercId)) localActiveMercId = null;

            if (string.IsNullOrEmpty(ownedStr)) return;

            string[] ids = ownedStr.Split(',');
            foreach (string id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                string prefix = $"Merc_{id}_";

                long.TryParse(PlayerPrefs.GetString(prefix + "Exp", "0"), out long mercExp);
                ownedMercenaries[id] = new MercenarySaveData
                {
                    mercId = id,
                    level = PlayerPrefs.GetInt(prefix + "Level", 1),
                    exp = mercExp,
                    loyalty = PlayerPrefs.GetInt(prefix + "Loyalty", 10),
                    behavior = (MercBehavior)PlayerPrefs.GetInt(prefix + "Behavior", 0),
                    equippedWeapon = PlayerPrefs.GetString(prefix + "Weapon", ""),
                    equippedArmor = PlayerPrefs.GetString(prefix + "Armor", "")
                };
            }
        }

        #endregion

        #region Utility

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnMercenaryHired = null;
                OnMercenaryLevelUp = null;
                OnMercenaryActivated = null;
                OnMercenaryDismissed = null;
                OnBehaviorChanged = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    #region 데이터 구조체

    public enum MercenaryType
    {
        Warrior,
        Archer,
        Mage,
        Healer,
        Assassin,
        Guardian
    }

    public enum MercBehavior
    {
        Balanced,       // 균형 (기본)
        Aggressive,     // 공격적 (데미지 +30%, 방어 -30%)
        Defensive,      // 방어적 (방어 +30%, 데미지 -30%)
        Support         // 지원 (버프/힐 우선, 데미지 -50%)
    }

    [System.Serializable]
    public struct MercStat
    {
        public float hp;
        public float atk;
        public float def;
        public float mdef;
        public float spd;
    }

    [System.Serializable]
    public class MercSkillInfo
    {
        public string name;
        public string desc;
        public float cooldown;
        public float damagePercent;
    }

    [System.Serializable]
    public class MercTrait
    {
        public string name;
        public string desc;
        public int requiredLevel;
        public string statType;
        public float value;
    }

    [System.Serializable]
    public class MercenaryTemplate
    {
        public string id;
        public string name;
        public MercenaryType type;
        public string description;
        public MercStat baseStat;
        public MercStat growthStat;
        public long hireCost;
        public long dailyCost;
        public MercSkillInfo[] skills;
        public MercTrait[] traits;
    }

    public class ActiveMercenary
    {
        public string mercId;
        public float currentHP;
        public MercBehavior behavior;
        public float[] skillCooldowns;
    }

    [System.Serializable]
    public class MercenarySaveData
    {
        public string mercId;
        public int level;
        public long exp;
        public int loyalty;
        public MercBehavior behavior;
        public string equippedWeapon;
        public string equippedArmor;
    }

    #endregion
}
