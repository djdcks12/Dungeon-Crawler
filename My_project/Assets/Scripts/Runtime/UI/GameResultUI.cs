using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 던전 클리어/실패 결과 화면
    /// 보상, 통계 (처치 수, 최고층, 소요시간) 표시
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        public static GameResultUI Instance { get; private set; }

        // UI 요소
        private Canvas resultCanvas;
        private GameObject panelObj;
        private Text titleText;
        private Text statsText;
        private Text rewardsText;
        private Text detailsText;
        private Button confirmButton;

        private bool isVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateUI();
        }

        /// <summary>
        /// 던전 클리어 결과 표시
        /// </summary>
        public void ShowClearResult(DungeonResultData data)
        {
            data.isCleared = true;
            ShowResult(data);
        }

        /// <summary>
        /// 던전 실패 결과 표시
        /// </summary>
        public void ShowFailResult(DungeonResultData data)
        {
            data.isCleared = false;
            ShowResult(data);
        }

        private void ShowResult(DungeonResultData data)
        {
            if (panelObj == null) return;

            isVisible = true;
            panelObj.SetActive(true);

            // 타이틀
            if (data.isCleared)
            {
                titleText.text = "<color=#FFD700>DUNGEON CLEAR!</color>";
                titleText.color = new Color(1f, 0.84f, 0f);
            }
            else
            {
                titleText.text = "<color=#FF4444>DUNGEON FAILED</color>";
                titleText.color = new Color(1f, 0.27f, 0.27f);
            }

            // 통계
            int minutes = (int)(data.elapsedTime / 60);
            int seconds = (int)(data.elapsedTime % 60);

            string stats = $"<color=#AADDFF><b>{data.dungeonName}</b></color>\n\n";
            stats += $"<color=#FFFFFF>도달 층수:</color> <color=#FFD700>{data.reachedFloor}/{data.totalFloors}</color>\n";
            stats += $"<color=#FFFFFF>소요 시간:</color> <color=#FFD700>{minutes:D2}:{seconds:D2}</color>\n";
            stats += $"<color=#FFFFFF>몬스터 처치:</color> <color=#FF6666>{data.monstersKilled}</color>\n";
            stats += $"<color=#FFFFFF>받은 피해:</color> <color=#FF8888>{data.damageTaken:N0}</color>\n";
            stats += $"<color=#FFFFFF>가한 피해:</color> <color=#66FF66>{data.damageDealt:N0}</color>\n";

            if (data.bossesKilled > 0)
                stats += $"<color=#FFFFFF>보스 처치:</color> <color=#FF66FF>{data.bossesKilled}</color>\n";

            if (data.deathCount > 0)
                stats += $"<color=#FFFFFF>사망 횟수:</color> <color=#FF4444>{data.deathCount}</color>\n";

            statsText.text = stats;

            // 보상
            string rewards = "<color=#FFD700><b>== 보상 ==</b></color>\n\n";

            if (data.expReward > 0)
                rewards += $"<color=#AADDFF>경험치:</color> +{data.expReward:N0}\n";

            if (data.goldReward > 0)
                rewards += $"<color=#FFD700>골드:</color> +{data.goldReward:N0}\n";

            if (data.isCleared && data.completionBonus > 1f)
            {
                rewards += $"\n<color=#66FF66>완주 보너스: x{data.completionBonus:F1}</color>\n";
            }

            // 드롭 아이템
            if (data.droppedItems != null && data.droppedItems.Count > 0)
            {
                rewards += $"\n<color=#FFD700>획득 아이템 ({data.droppedItems.Count}개):</color>\n";
                int shown = 0;
                foreach (var item in data.droppedItems)
                {
                    if (shown >= 8)
                    {
                        rewards += $"  ... 외 {data.droppedItems.Count - 8}개\n";
                        break;
                    }
                    string gradeColor = GetGradeColor(item.grade);
                    rewards += $"  <color={gradeColor}>{item.itemName}</color>";
                    if (item.quantity > 1) rewards += $" x{item.quantity}";
                    rewards += "\n";
                    shown++;
                }
            }

            rewardsText.text = rewards;

            // 세부 정보
            string details = "";

            // 등급 평가
            string grade = EvaluatePerformance(data);
            details += $"\n<color=#FFD700>종합 평가: {grade}</color>";

            if (data.isCleared)
            {
                if (data.isFirstClear)
                    details += "\n<color=#FF66FF>첫 클리어 보너스!</color>";

                if (data.isNewRecord)
                    details += "\n<color=#66FFFF>신기록 달성!</color>";
            }

            detailsText.text = details;
        }

        private string EvaluatePerformance(DungeonResultData data)
        {
            int score = 0;

            // 층수 달성률
            float floorRatio = (float)data.reachedFloor / Mathf.Max(1, data.totalFloors);
            score += (int)(floorRatio * 40); // 최대 40점

            // 사망 횟수 (적을수록 좋음)
            score += Mathf.Max(0, 20 - data.deathCount * 5); // 최대 20점

            // 처치 수
            score += Mathf.Min(20, data.monstersKilled / 2); // 최대 20점

            // 시간 (빠를수록 보너스)
            if (data.isCleared)
            {
                float timeRatio = data.elapsedTime / Mathf.Max(1, data.timeLimitSeconds);
                if (timeRatio < 0.5f) score += 20;
                else if (timeRatio < 0.75f) score += 15;
                else if (timeRatio < 1.0f) score += 10;
                else score += 5;
            }

            if (score >= 90) return "<color=#FFD700>S</color>";
            if (score >= 75) return "<color=#FF66FF>A</color>";
            if (score >= 60) return "<color=#6666FF>B</color>";
            if (score >= 40) return "<color=#66FF66>C</color>";
            if (score >= 20) return "<color=#FFAA66>D</color>";
            return "<color=#FF4444>F</color>";
        }

        private string GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => "#AAAAAA",
                ItemGrade.Uncommon => "#44FF44",
                ItemGrade.Rare => "#4488FF",
                ItemGrade.Epic => "#AA44FF",
                ItemGrade.Legendary => "#FF8800",
                _ => "#FFFFFF"
            };
        }

        public void Hide()
        {
            isVisible = false;
            if (panelObj != null) panelObj.SetActive(false);
        }

        private void Update()
        {
            if (isVisible && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)))
            {
                Hide();
            }
        }

        private void CreateUI()
        {
            // Canvas
            var canvasObj = new GameObject("ResultCanvas");
            canvasObj.transform.SetParent(transform, false);
            resultCanvas = canvasObj.AddComponent<Canvas>();
            resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            resultCanvas.sortingOrder = 200;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 반투명 배경
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 메인 패널
            panelObj = new GameObject("ResultPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.06f, 0.12f, 0.95f);
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(700, 550);

            // 타이틀
            titleText = CreateText(panelObj.transform, "Title", "", 28, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -10), new Vector2(0, 50));

            // 통계 (좌측)
            statsText = CreateText(panelObj.transform, "Stats", "", 14, FontStyle.Normal,
                TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(0.5f, 1),
                new Vector2(20, 60), new Vector2(-10, -65));

            // 보상 (우측)
            rewardsText = CreateText(panelObj.transform, "Rewards", "", 14, FontStyle.Normal,
                TextAnchor.UpperLeft, new Vector2(0.5f, 0), new Vector2(1, 1),
                new Vector2(10, 60), new Vector2(-20, -65));

            // 세부 정보 (하단)
            detailsText = CreateText(panelObj.transform, "Details", "", 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(10, 10), new Vector2(-10, 50));

            // 확인 버튼
            var btnObj = new GameObject("ConfirmBtn");
            btnObj.transform.SetParent(panelObj.transform, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.4f, 0.6f);
            confirmButton = btnObj.AddComponent<Button>();
            confirmButton.onClick.AddListener(Hide);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.pivot = new Vector2(0.5f, 0);
            btnRt.sizeDelta = new Vector2(120, 35);
            btnRt.anchoredPosition = new Vector2(0, 55);

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnText = btnTextObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 16;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "확인 (Enter)";
            var btRt = btnTextObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            panelObj.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string content,
            int fontSize, FontStyle style, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.text = content;
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return text;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 던전 결과 데이터
    /// </summary>
    [System.Serializable]
    public class DungeonResultData
    {
        public string dungeonName = "";
        public bool isCleared = false;

        // 통계
        public int reachedFloor = 0;
        public int totalFloors = 10;
        public float elapsedTime = 0f;
        public float timeLimitSeconds = 600f;
        public int monstersKilled = 0;
        public int bossesKilled = 0;
        public float damageDealt = 0f;
        public float damageTaken = 0f;
        public int deathCount = 0;

        // 보상
        public long expReward = 0;
        public long goldReward = 0;
        public float completionBonus = 1f;

        // 아이템
        public List<DroppedItemInfo> droppedItems = new List<DroppedItemInfo>();

        // 특수
        public bool isFirstClear = false;
        public bool isNewRecord = false;
    }

    /// <summary>
    /// 드롭 아이템 정보 (결과 표시용)
    /// </summary>
    [System.Serializable]
    public struct DroppedItemInfo
    {
        public string itemName;
        public ItemGrade grade;
        public int quantity;
    }
}
