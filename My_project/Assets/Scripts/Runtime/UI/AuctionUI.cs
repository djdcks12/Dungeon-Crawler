using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 경매장 UI - 등록/검색/내 경매/거래 내역
    /// </summary>
    public class AuctionUI : MonoBehaviour
    {
        private GameObject mainPanel;
        private InputField searchInput;
        private Transform listContent;
        private List<GameObject> listEntries = new List<GameObject>();
        private Text statusText;

        private int currentTab; // 0=검색, 1=내 경매
        private bool isVisible;

        private void Start()
        {
            BuildUI();
            mainPanel.SetActive(false);

            var auction = AuctionSystem.Instance;
            if (auction != null)
            {
                auction.OnListingsUpdated += OnListingsUpdated;
                auction.OnAuctionMessage += OnAuctionMessage;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Y))
                ToggleUI();
        }

        public void ToggleUI()
        {
            isVisible = !isVisible;
            mainPanel.SetActive(isVisible);
            if (isVisible)
                DoSearch();
        }

        private void DoSearch()
        {
            string term = searchInput != null ? searchInput.text : "";
            AuctionSystem.Instance?.SearchListingsServerRpc(term, 0);
        }

        private void ShowMyListings()
        {
            currentTab = 1;
            AuctionSystem.Instance?.GetMyListingsServerRpc();
        }

        private void OnListingsUpdated(List<AuctionListing> listings)
        {
            ClearList();

            if (listings.Count == 0)
            {
                statusText.text = "등록된 경매가 없습니다.";
                return;
            }

            statusText.text = $"{listings.Count}건 검색됨";

            foreach (var listing in listings)
            {
                var entry = CreateListingEntry(listing);
                listEntries.Add(entry);
            }
        }

        private void OnAuctionMessage(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void ClearList()
        {
            foreach (var e in listEntries)
                Destroy(e);
            listEntries.Clear();
        }

        private GameObject CreateListingEntry(AuctionListing listing)
        {
            var go = new GameObject($"Listing_{listing.listingId}");
            go.transform.SetParent(listContent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(5, 5, 2, 2);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 35;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.7f);

            // 등급 색상
            Color gradeColor = ItemData.GetGradeColor((ItemGrade)listing.itemGrade);

            // 아이템 이름
            CreateCellText(go.transform, listing.itemName.ToString(), 150, gradeColor);

            // 수량
            CreateCellText(go.transform, $"x{listing.quantity}", 40, Color.gray);

            // 현재 입찰가
            string bidText = listing.currentBid > 0 ? $"{listing.currentBid}G" : $"{listing.startPrice}G";
            CreateCellText(go.transform, bidText, 80, Color.yellow);

            // 즉구가
            string buyoutText = listing.buyoutPrice > 0 ? $"{listing.buyoutPrice}G" : "-";
            CreateCellText(go.transform, buyoutText, 80, new Color(1f, 0.6f, 0f));

            // 판매자
            CreateCellText(go.transform, listing.sellerName.ToString(), 80, Color.white);

            // 즉시구매 버튼
            if (listing.buyoutPrice > 0)
            {
                int id = listing.listingId;
                CreateCellButton(go.transform, "구매", 50, () =>
                {
                    AuctionSystem.Instance?.BuyoutServerRpc(id);
                });
            }

            return go;
        }

        #region UI Build

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 110;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            mainPanel = CreatePanel("AuctionPanel", transform, new Vector2(650, 500));

            // 타이틀
            CreateLabel(mainPanel.transform, "경매장", new Vector2(0, -20), 22, Color.white);

            // 탭 버튼
            CreateBtn(mainPanel.transform, "검색", new Vector2(-200, -55), new Vector2(80, 28), () => { currentTab = 0; DoSearch(); });
            CreateBtn(mainPanel.transform, "내 경매", new Vector2(-110, -55), new Vector2(80, 28), ShowMyListings);

            // 검색 바
            var searchGo = new GameObject("SearchInput");
            searchGo.transform.SetParent(mainPanel.transform, false);
            var searchRect = searchGo.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0.5f, 0.5f);
            searchRect.anchorMax = new Vector2(0.5f, 0.5f);
            searchRect.anchoredPosition = new Vector2(50, -55);
            searchRect.sizeDelta = new Vector2(250, 28);
            var searchImg = searchGo.AddComponent<Image>();
            searchImg.color = new Color(0.2f, 0.2f, 0.25f);
            searchInput = searchGo.AddComponent<InputField>();
            searchInput.characterLimit = 30;

            var searchTextGo = CreateLabel(searchGo.transform, "", Vector2.zero, 13, Color.white);
            var sTextRect = searchTextGo.GetComponent<RectTransform>();
            sTextRect.anchorMin = Vector2.zero;
            sTextRect.anchorMax = Vector2.one;
            sTextRect.offsetMin = new Vector2(5, 0);
            sTextRect.offsetMax = new Vector2(-5, 0);
            searchInput.textComponent = searchTextGo.GetComponent<Text>();

            CreateBtn(mainPanel.transform, "검색", new Vector2(210, -55), new Vector2(60, 28), DoSearch);

            // 헤더
            var header = new GameObject("Header");
            header.transform.SetParent(mainPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 0.5f);
            headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0, -85);
            headerRect.sizeDelta = new Vector2(620, 25);

            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 5;
            headerLayout.childForceExpandWidth = false;
            headerLayout.padding = new RectOffset(5, 5, 0, 0);

            CreateCellText(header.transform, "아이템", 150, Color.gray);
            CreateCellText(header.transform, "수량", 40, Color.gray);
            CreateCellText(header.transform, "현재가", 80, Color.gray);
            CreateCellText(header.transform, "즉구가", 80, Color.gray);
            CreateCellText(header.transform, "판매자", 80, Color.gray);

            // 목록 스크롤
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0, -230);
            scrollRect.sizeDelta = new Vector2(620, 280);

            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0.1f, 0.1f, 0.12f, 0.5f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(2, 2, 2, 2);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;
            listContent = contentGo.transform;

            // 상태 텍스트
            var statusGo = CreateLabel(mainPanel.transform, "Y키로 열기/닫기", new Vector2(0, -390), 12, Color.gray);
            statusText = statusGo.GetComponent<Text>();

            // 닫기 버튼
            CreateBtn(mainPanel.transform, "X", new Vector2(305, -15), new Vector2(30, 30), () => { isVisible = false; mainPanel.SetActive(false); });
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            return go;
        }

        private GameObject CreateLabel(Transform parent, string text, Vector2 pos, int fontSize, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 30);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        private void CreateBtn(Transform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(go.transform, false);
            var tRect = tGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            var t = tGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.color = Color.white;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateCellText(Transform parent, string text, float width, Color color)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.color = color;
            t.text = text;
            t.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateCellButton(Transform parent, string text, float width, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"CellBtn_{text}");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 28;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.5f, 0.3f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(go.transform, false);
            var tRect = tGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            var t = tGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 11;
            t.color = Color.white;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        private void OnDestroy()
        {
            var auction = AuctionSystem.Instance;
            if (auction != null)
            {
                auction.OnListingsUpdated -= OnListingsUpdated;
                auction.OnAuctionMessage -= OnAuctionMessage;
            }
        }
    }
}
