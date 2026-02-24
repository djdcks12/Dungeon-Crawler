using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 루트 필터 시스템 - 아이템 표시/숨김/자동 줍기 규칙
    /// 등급별, 타입별 필터링
    /// 프리셋 3개 저장 (전투/파밍/크래프팅)
    /// </summary>
    public class LootFilterSystem : MonoBehaviour
    {
        public static LootFilterSystem Instance { get; private set; }

        [Header("기본 설정")]
        [SerializeField] private bool filterEnabled = true;
        [SerializeField] private int maxPresets = 3;

        // 현재 활성 프리셋
        private int activePresetIndex = 0;
        private LootFilterPreset[] presets;

        // 이벤트
        public System.Action<int> OnPresetChanged; // presetIndex
        public System.Action OnFilterUpdated;

        // 접근자
        public bool IsEnabled => filterEnabled;
        public int ActivePresetIndex => activePresetIndex;
        public LootFilterPreset ActivePreset => presets != null && activePresetIndex < presets.Length ? presets[activePresetIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializePresets();
            LoadPresets();
        }

        #region 초기화

        private void InitializePresets()
        {
            presets = new LootFilterPreset[maxPresets];

            // 프리셋 0: 전투 (커먼 숨김, 소모품/재료 자동줍기)
            presets[0] = new LootFilterPreset
            {
                name = "전투",
                gradeRules = new Dictionary<ItemGrade, LootFilterAction>
                {
                    { ItemGrade.Common, LootFilterAction.Hide },
                    { ItemGrade.Uncommon, LootFilterAction.Show },
                    { ItemGrade.Rare, LootFilterAction.Highlight },
                    { ItemGrade.Epic, LootFilterAction.Highlight },
                    { ItemGrade.Legendary, LootFilterAction.HighlightBeam }
                },
                typeRules = new Dictionary<ItemType, LootFilterAction>
                {
                    { ItemType.Equipment, LootFilterAction.Show },
                    { ItemType.Consumable, LootFilterAction.AutoPickup },
                    { ItemType.Material, LootFilterAction.AutoPickup },
                    { ItemType.Quest, LootFilterAction.HighlightBeam }
                },
                autoPickupGold = true,
                autoPickupRadius = 3f,
                showDroppedItemName = true,
                minGradeForNotification = ItemGrade.Rare
            };

            // 프리셋 1: 파밍 (전부 줍기, 골드 자동)
            presets[1] = new LootFilterPreset
            {
                name = "파밍",
                gradeRules = new Dictionary<ItemGrade, LootFilterAction>
                {
                    { ItemGrade.Common, LootFilterAction.AutoPickup },
                    { ItemGrade.Uncommon, LootFilterAction.AutoPickup },
                    { ItemGrade.Rare, LootFilterAction.Highlight },
                    { ItemGrade.Epic, LootFilterAction.HighlightBeam },
                    { ItemGrade.Legendary, LootFilterAction.HighlightBeam }
                },
                typeRules = new Dictionary<ItemType, LootFilterAction>
                {
                    { ItemType.Equipment, LootFilterAction.AutoPickup },
                    { ItemType.Consumable, LootFilterAction.AutoPickup },
                    { ItemType.Material, LootFilterAction.AutoPickup },
                    { ItemType.Quest, LootFilterAction.HighlightBeam }
                },
                autoPickupGold = true,
                autoPickupRadius = 5f,
                showDroppedItemName = true,
                minGradeForNotification = ItemGrade.Epic
            };

            // 프리셋 2: 크래프팅 (재료 하이라이트, 장비 숨김)
            presets[2] = new LootFilterPreset
            {
                name = "크래프팅",
                gradeRules = new Dictionary<ItemGrade, LootFilterAction>
                {
                    { ItemGrade.Common, LootFilterAction.Hide },
                    { ItemGrade.Uncommon, LootFilterAction.Show },
                    { ItemGrade.Rare, LootFilterAction.Highlight },
                    { ItemGrade.Epic, LootFilterAction.Highlight },
                    { ItemGrade.Legendary, LootFilterAction.HighlightBeam }
                },
                typeRules = new Dictionary<ItemType, LootFilterAction>
                {
                    { ItemType.Equipment, LootFilterAction.Hide },
                    { ItemType.Consumable, LootFilterAction.Show },
                    { ItemType.Material, LootFilterAction.HighlightBeam },
                    { ItemType.Quest, LootFilterAction.HighlightBeam }
                },
                autoPickupGold = true,
                autoPickupRadius = 4f,
                showDroppedItemName = false,
                minGradeForNotification = ItemGrade.Rare
            };
        }

        #endregion

        #region 필터 조회

        /// <summary>
        /// 아이템에 적용할 필터 액션 결정
        /// 타입 규칙이 등급 규칙보다 우선
        /// </summary>
        public LootFilterAction GetFilterAction(ItemData itemData)
        {
            if (!filterEnabled || itemData == null || ActivePreset == null)
                return LootFilterAction.Show;

            var preset = ActivePreset;

            // 1. 타입별 규칙 확인 (우선)
            if (preset.typeRules.TryGetValue(itemData.ItemType, out var typeAction))
            {
                // 타입이 AutoPickup이면 등급으로 업그레이드 가능
                if (typeAction == LootFilterAction.AutoPickup)
                {
                    // 높은 등급은 하이라이트로 표시
                    if (preset.gradeRules.TryGetValue(itemData.Grade, out var gradeAction))
                    {
                        if (gradeAction == LootFilterAction.HighlightBeam || gradeAction == LootFilterAction.Highlight)
                            return gradeAction;
                    }
                    return LootFilterAction.AutoPickup;
                }

                // 타입이 Hide이면 등급으로 오버라이드 가능 (레전더리는 절대 숨기지 않음)
                if (typeAction == LootFilterAction.Hide)
                {
                    if (itemData.Grade == ItemGrade.Legendary)
                        return LootFilterAction.HighlightBeam;
                    if (itemData.Grade == ItemGrade.Epic)
                        return LootFilterAction.Show;
                    return LootFilterAction.Hide;
                }

                return typeAction;
            }

            // 2. 등급별 규칙 확인
            if (preset.gradeRules.TryGetValue(itemData.Grade, out var action))
                return action;

            return LootFilterAction.Show;
        }

        /// <summary>
        /// 아이템을 표시할지 여부
        /// </summary>
        public bool ShouldShowItem(ItemData itemData)
        {
            var action = GetFilterAction(itemData);
            return action != LootFilterAction.Hide;
        }

        /// <summary>
        /// 자동 줍기 대상인지 여부
        /// </summary>
        public bool ShouldAutoPickup(ItemData itemData)
        {
            var action = GetFilterAction(itemData);
            return action == LootFilterAction.AutoPickup;
        }

        /// <summary>
        /// 하이라이트 표시할지 여부
        /// </summary>
        public bool ShouldHighlight(ItemData itemData)
        {
            var action = GetFilterAction(itemData);
            return action == LootFilterAction.Highlight || action == LootFilterAction.HighlightBeam;
        }

        /// <summary>
        /// 빔 이펙트 표시할지 여부
        /// </summary>
        public bool ShouldShowBeam(ItemData itemData)
        {
            return GetFilterAction(itemData) == LootFilterAction.HighlightBeam;
        }

        /// <summary>
        /// 골드 자동 줍기 여부
        /// </summary>
        public bool ShouldAutoPickupGold()
        {
            return filterEnabled && ActivePreset != null && ActivePreset.autoPickupGold;
        }

        /// <summary>
        /// 자동 줍기 반경
        /// </summary>
        public float GetAutoPickupRadius()
        {
            return ActivePreset?.autoPickupRadius ?? 2f;
        }

        /// <summary>
        /// 드롭 아이템 이름 표시 여부
        /// </summary>
        public bool ShouldShowItemName()
        {
            return ActivePreset?.showDroppedItemName ?? true;
        }

        /// <summary>
        /// 드롭 알림을 표시할 등급인지 확인
        /// </summary>
        public bool ShouldNotifyDrop(ItemData itemData)
        {
            if (!filterEnabled || ActivePreset == null || itemData == null) return false;
            return (int)itemData.Grade >= (int)ActivePreset.minGradeForNotification;
        }

        /// <summary>
        /// 등급에 따른 하이라이트 색상
        /// </summary>
        public Color GetHighlightColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => new Color(0.7f, 0.7f, 0.7f),
                ItemGrade.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                ItemGrade.Rare => new Color(0.2f, 0.4f, 1f),
                ItemGrade.Epic => new Color(0.6f, 0.2f, 0.8f),
                ItemGrade.Legendary => new Color(1f, 0.6f, 0f),
                _ => Color.white
            };
        }

        #endregion

        #region 프리셋 관리

        /// <summary>
        /// 활성 프리셋 변경
        /// </summary>
        public void SetActivePreset(int index)
        {
            if (index < 0 || index >= maxPresets) return;
            activePresetIndex = index;
            PlayerPrefs.SetInt("LootFilter_ActivePreset", index);
            PlayerPrefs.Save();

            OnPresetChanged?.Invoke(index);
            OnFilterUpdated?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"루트 필터: {presets[index].name}", NotificationType.System);
        }

        /// <summary>
        /// 다음 프리셋으로 전환
        /// </summary>
        public void CyclePreset()
        {
            if (maxPresets <= 0) maxPresets = 3;
            SetActivePreset((activePresetIndex + 1) % maxPresets);
        }

        /// <summary>
        /// 프리셋 이름 변경
        /// </summary>
        public void RenamePreset(int index, string newName)
        {
            if (index < 0 || index >= maxPresets) return;
            presets[index].name = newName;
            SavePresets();
        }

        /// <summary>
        /// 필터 활성/비활성 토글
        /// </summary>
        public void ToggleFilter()
        {
            filterEnabled = !filterEnabled;
            PlayerPrefs.SetInt("LootFilter_Enabled", filterEnabled ? 1 : 0);
            PlayerPrefs.Save();
            OnFilterUpdated?.Invoke();
        }

        /// <summary>
        /// 프리셋 정보 조회
        /// </summary>
        public LootFilterPreset GetPreset(int index)
        {
            if (index < 0 || index >= maxPresets) return null;
            return presets[index];
        }

        /// <summary>
        /// 모든 프리셋 이름 목록
        /// </summary>
        public string[] GetPresetNames()
        {
            string[] names = new string[maxPresets];
            for (int i = 0; i < maxPresets; i++)
                names[i] = presets[i]?.name ?? $"프리셋 {i + 1}";
            return names;
        }

        #endregion

        #region 규칙 수정

        /// <summary>
        /// 등급별 필터 규칙 변경
        /// </summary>
        public void SetGradeRule(int presetIndex, ItemGrade grade, LootFilterAction action)
        {
            if (presetIndex < 0 || presetIndex >= maxPresets) return;
            presets[presetIndex].gradeRules[grade] = action;
            SavePresets();
            if (presetIndex == activePresetIndex) OnFilterUpdated?.Invoke();
        }

        /// <summary>
        /// 타입별 필터 규칙 변경
        /// </summary>
        public void SetTypeRule(int presetIndex, ItemType type, LootFilterAction action)
        {
            if (presetIndex < 0 || presetIndex >= maxPresets) return;
            presets[presetIndex].typeRules[type] = action;
            SavePresets();
            if (presetIndex == activePresetIndex) OnFilterUpdated?.Invoke();
        }

        /// <summary>
        /// 골드 자동 줍기 설정
        /// </summary>
        public void SetAutoPickupGold(int presetIndex, bool enabled)
        {
            if (presetIndex < 0 || presetIndex >= maxPresets) return;
            presets[presetIndex].autoPickupGold = enabled;
            SavePresets();
        }

        /// <summary>
        /// 자동 줍기 반경 설정
        /// </summary>
        public void SetAutoPickupRadius(int presetIndex, float radius)
        {
            if (presetIndex < 0 || presetIndex >= maxPresets) return;
            presets[presetIndex].autoPickupRadius = Mathf.Clamp(radius, 1f, 10f);
            SavePresets();
        }

        /// <summary>
        /// 드롭 알림 최소 등급 설정
        /// </summary>
        public void SetMinNotificationGrade(int presetIndex, ItemGrade grade)
        {
            if (presetIndex < 0 || presetIndex >= maxPresets) return;
            presets[presetIndex].minGradeForNotification = grade;
            SavePresets();
        }

        #endregion

        #region 저장/로드

        private void SavePresets()
        {
            for (int i = 0; i < maxPresets; i++)
            {
                var p = presets[i];
                string prefix = $"LootFilter_P{i}_";

                PlayerPrefs.SetString(prefix + "Name", p.name);
                PlayerPrefs.SetInt(prefix + "AutoGold", p.autoPickupGold ? 1 : 0);
                PlayerPrefs.SetFloat(prefix + "Radius", p.autoPickupRadius);
                PlayerPrefs.SetInt(prefix + "ShowName", p.showDroppedItemName ? 1 : 0);
                PlayerPrefs.SetInt(prefix + "MinNotif", (int)p.minGradeForNotification);

                // 등급 규칙
                foreach (var kvp in p.gradeRules)
                    PlayerPrefs.SetInt(prefix + "Grade_" + (int)kvp.Key, (int)kvp.Value);

                // 타입 규칙
                foreach (var kvp in p.typeRules)
                    PlayerPrefs.SetInt(prefix + "Type_" + (int)kvp.Key, (int)kvp.Value);
            }
            PlayerPrefs.Save();
        }

        private void LoadPresets()
        {
            activePresetIndex = PlayerPrefs.GetInt("LootFilter_ActivePreset", 0);
            filterEnabled = PlayerPrefs.GetInt("LootFilter_Enabled", 1) == 1;

            for (int i = 0; i < maxPresets; i++)
            {
                string prefix = $"LootFilter_P{i}_";

                if (PlayerPrefs.HasKey(prefix + "Name"))
                {
                    presets[i].name = PlayerPrefs.GetString(prefix + "Name", presets[i].name);
                    presets[i].autoPickupGold = PlayerPrefs.GetInt(prefix + "AutoGold", 1) == 1;
                    presets[i].autoPickupRadius = PlayerPrefs.GetFloat(prefix + "Radius", presets[i].autoPickupRadius);
                    presets[i].showDroppedItemName = PlayerPrefs.GetInt(prefix + "ShowName", 1) == 1;
                    presets[i].minGradeForNotification = (ItemGrade)PlayerPrefs.GetInt(prefix + "MinNotif", (int)presets[i].minGradeForNotification);

                    // 등급 규칙 로드
                    var gradeValues = System.Enum.GetValues(typeof(ItemGrade));
                    foreach (ItemGrade grade in gradeValues)
                    {
                        string key = prefix + "Grade_" + (int)grade;
                        if (PlayerPrefs.HasKey(key))
                            presets[i].gradeRules[grade] = (LootFilterAction)PlayerPrefs.GetInt(key);
                    }

                    // 타입 규칙 로드
                    var typeValues = System.Enum.GetValues(typeof(ItemType));
                    foreach (ItemType type in typeValues)
                    {
                        string key = prefix + "Type_" + (int)type;
                        if (PlayerPrefs.HasKey(key))
                            presets[i].typeRules[type] = (LootFilterAction)PlayerPrefs.GetInt(key);
                    }
                }
            }
        }

        #endregion

        private void Update()
        {
            // F6키로 프리셋 순환
            if (Input.GetKeyDown(KeyCode.F6))
            {
                CyclePreset();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnPresetChanged = null;
                OnFilterUpdated = null;
                Instance = null;
            }
        }
    }

    #region 데이터 구조체

    public enum LootFilterAction
    {
        Show,           // 기본 표시
        Hide,           // 숨김
        Highlight,      // 하이라이트 (색상 강조)
        HighlightBeam,  // 빔 이펙트 (빛기둥)
        AutoPickup      // 자동 줍기
    }

    [System.Serializable]
    public class LootFilterPreset
    {
        public string name;
        public Dictionary<ItemGrade, LootFilterAction> gradeRules = new Dictionary<ItemGrade, LootFilterAction>();
        public Dictionary<ItemType, LootFilterAction> typeRules = new Dictionary<ItemType, LootFilterAction>();
        public bool autoPickupGold;
        public float autoPickupRadius;
        public bool showDroppedItemName;
        public ItemGrade minGradeForNotification;
    }

    #endregion
}
