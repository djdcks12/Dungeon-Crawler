using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 도움말 UI - H키 토글
    /// 시스템별 도움말, 조작법, 스탯/직업 설명
    /// </summary>
    public class HelpUI : MonoBehaviour
    {
        public static HelpUI Instance { get; private set; }

        // UI
        private Canvas helpCanvas;
        private GameObject panelObj;
        private Text contentText;
        private int currentPage = 0;
        private bool isVisible = false;

        private List<HelpPage> pages = new List<HelpPage>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePages();
            CreateUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                Toggle();
            }

            if (!isVisible) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                PreviousPage();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                NextPage();
            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        public void Toggle()
        {
            if (isVisible) Hide();
            else Show();
        }

        public void Show()
        {
            isVisible = true;
            if (panelObj != null) panelObj.SetActive(true);
            RefreshPage();
        }

        public void Hide()
        {
            isVisible = false;
            if (panelObj != null) panelObj.SetActive(false);
        }

        public void NextPage()
        {
            currentPage = (currentPage + 1) % pages.Count;
            RefreshPage();
        }

        public void PreviousPage()
        {
            currentPage = (currentPage - 1 + pages.Count) % pages.Count;
            RefreshPage();
        }

        private void RefreshPage()
        {
            if (contentText == null || currentPage < 0 || currentPage >= pages.Count)
                return;

            var page = pages[currentPage];
            contentText.text = $"<color=#FFD700><b>{page.title}</b></color>\n" +
                              $"<color=#888888>({currentPage + 1}/{pages.Count}) A/D 키로 페이지 이동</color>\n\n" +
                              page.content;
        }

        private void InitializePages()
        {
            pages.Clear();

            // 1. 기본 조작
            pages.Add(new HelpPage
            {
                title = "기본 조작",
                content =
                    "<color=#AADDFF>이동</color>: WASD / 화살표 키\n" +
                    "<color=#AADDFF>기본 공격</color>: 마우스 좌클릭\n" +
                    "<color=#AADDFF>스킬 사용</color>: 숫자키 1 ~ 5\n" +
                    "<color=#AADDFF>NPC 상호작용</color>: F 키\n" +
                    "<color=#AADDFF>자동 전투</color>: V 키\n\n" +
                    "<color=#AADDFF>인벤토리</color>: I 키\n" +
                    "<color=#AADDFF>퀘스트</color>: J 키\n" +
                    "<color=#AADDFF>미니맵</color>: M 키\n" +
                    "<color=#AADDFF>채팅</color>: Enter 키\n" +
                    "<color=#AADDFF>도움말</color>: H 키 (이 화면)\n" +
                    "<color=#AADDFF>ESC</color>: 닫기"
            });

            // 2. 스탯 설명
            pages.Add(new HelpPage
            {
                title = "스탯 설명",
                content =
                    "<color=#FF6666>STR (힘)</color>: 물리 공격력 증가\n" +
                    "<color=#66FF66>AGI (민첩)</color>: 이동속도, 공격속도, 회피율 증가\n" +
                    "<color=#FFAA66>VIT (체력)</color>: 최대 HP 증가 (VIT x 10)\n" +
                    "<color=#6666FF>INT (지능)</color>: 마법 공격력, 최대 MP 증가\n" +
                    "<color=#AAAAAA>DEF (방어력)</color>: 물리 피해 감소\n" +
                    "<color=#AA66FF>MDEF (마방)</color>: 마법 피해 감소\n" +
                    "<color=#FFFF66>LUK (운)</color>: 치명타 확률 증가\n" +
                    "<color=#66FFFF>STAB (안정성)</color>: 최소 데미지 상향\n\n" +
                    "<color=#888888>데미지 공식: DEF / (DEF + 100) = 감소율</color>"
            });

            // 3. 종족 & 직업
            pages.Add(new HelpPage
            {
                title = "종족 & 직업 시스템",
                content =
                    "<color=#FFD700>종족 (4종)</color>\n" +
                    "  Human: 균형형, 숙련도 보너스 10%\n" +
                    "  Elf: 마법/민첩 특화\n" +
                    "  Dwarf: 체력/방어 특화\n" +
                    "  Machina: 방어/안정성 특화\n\n" +
                    "<color=#FFD700>무기군 (8종) -> 직업 (16종)</color>\n" +
                    "  한손검: Navigator / Guardian\n" +
                    "  양손검: Berserker / Templar\n" +
                    "  양손도끼: ElementalBruiser / Scout\n" +
                    "  단검: Assassin / Duelist\n" +
                    "  활: Tracker / Sniper\n" +
                    "  지팡이: Mage / Warlock\n" +
                    "  완드: Cleric / Druid\n" +
                    "  격투: Trapper / Amplifier"
            });

            // 4. 전투 시스템
            pages.Add(new HelpPage
            {
                title = "전투 시스템",
                content =
                    "<color=#FF6666>데미지 시스템</color>\n" +
                    "  물리/마법 최소~최대 데미지 범위\n" +
                    "  안정성(STAB)이 높을수록 최소 데미지 상승\n" +
                    "  무기 숙련도(0~100)로 추가 보정\n\n" +
                    "<color=#FF6666>원소 타입</color>\n" +
                    "  물리, 마법, 화염, 빙결, 번개, 독, 암흑, 신성\n\n" +
                    "<color=#FF6666>상태이상</color>\n" +
                    "  독(DoT), 화상(DoT), 재생(HoT)\n" +
                    "  힘/속도/보호 버프, 광폭(STR+/DEF-)\n\n" +
                    "<color=#FF6666>아이템 강화</color>\n" +
                    "  +1~+10 강화 가능, +4부터 실패 확률\n" +
                    "  +9 이상 실패 시 파괴 확률 10%"
            });

            // 5. 던전 가이드
            pages.Add(new HelpPage
            {
                title = "던전 가이드",
                content =
                    "<color=#66FF66>고블린 동굴</color> (쉬움) 권장 Lv.1-5\n" +
                    "  초보자용, 10층, 고블린 변종\n\n" +
                    "<color=#6699FF>어둠의 숲</color> (보통) 권장 Lv.3-8\n" +
                    "  야수 + 오크 변종\n\n" +
                    "<color=#FF6666>언데드 지하묘지</color> (어려움) 권장 Lv.5-10\n" +
                    "  언데드 + 악마 변종\n\n" +
                    "<color=#FF66FF>드래곤의 둥지</color> (매우 어려움) 권장 Lv.8-13\n" +
                    "  원소 + 드래곤 변종\n\n" +
                    "<color=#FF0000>마왕의 영역</color> (악몽) 권장 Lv.10-15\n" +
                    "  악마 + 구조물 변종, 부활 불가!"
            });

            // 6. 채팅 명령어
            pages.Add(new HelpPage
            {
                title = "채팅 명령어",
                content =
                    "/help - 명령어 목록\n" +
                    "/w [이름] [메시지] - 귓속말\n" +
                    "/clear - 채팅 초기화\n\n" +
                    "<color=#00FFFF>파티</color>\n" +
                    "  /party create - 파티 생성\n" +
                    "  /party info - 파티 정보\n" +
                    "  /invite [이름] - 파티 초대\n" +
                    "  /leave - 파티 탈퇴\n\n" +
                    "<color=#FFAA00>거래</color>\n" +
                    "  /trade [이름] - 거래 요청\n" +
                    "  /trade accept - 거래 수락\n" +
                    "  /trade cancel - 거래 취소\n\n" +
                    "/stats - 내 스탯 | /pos - 위치\n" +
                    "/roll [최대] - 주사위 | /time - 서버 시간"
            });
        }

        private void CreateUI()
        {
            var canvasObj = new GameObject("HelpCanvas");
            canvasObj.transform.SetParent(transform, false);
            helpCanvas = canvasObj.AddComponent<Canvas>();
            helpCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            helpCanvas.sortingOrder = 160;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 반투명 배경
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 패널
            panelObj = new GameObject("HelpPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.18f, 0.95f);
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(600, 500);

            // 콘텐츠 텍스트
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panelObj.transform, false);
            contentText = contentObj.AddComponent<Text>();
            contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contentText.fontSize = 15;
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.color = Color.white;
            contentText.supportRichText = true;
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(20, 50);
            contentRt.offsetMax = new Vector2(-20, -15);

            // 이전 버튼
            CreatePageButton(panelObj.transform, "PrevBtn", "<", new Vector2(0, 0), new Vector2(60, 35), new Vector2(20, 10), PreviousPage);

            // 다음 버튼
            CreatePageButton(panelObj.transform, "NextBtn", ">", new Vector2(1, 0), new Vector2(60, 35), new Vector2(-20, 10), NextPage);

            // 닫기 버튼
            CreatePageButton(panelObj.transform, "CloseBtn", "X", new Vector2(1, 1), new Vector2(35, 35), new Vector2(-5, -5), Hide);

            panelObj.SetActive(false);
        }

        private void CreatePageButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.3f, 0.4f);
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = anchor;
            btnRt.anchorMax = anchor;
            btnRt.pivot = anchor;
            btnRt.sizeDelta = size;
            btnRt.anchoredPosition = pos;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            var tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    [System.Serializable]
    public struct HelpPage
    {
        public string title;
        public string content;
    }
}
