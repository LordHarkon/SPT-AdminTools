using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using AdminTools.Models;
using Newtonsoft.Json;
using SPT.Common.Utils;
using System;
using System.Linq;
using System.IO.Compression;
using System.IO;
using SPT.Common.Http;
using System.Threading.Tasks;
using System.Net.Http;
using EFT.UI;
using System.Text;

namespace AdminTools
{
    [BepInPlugin("com.spt.admintools", "Admin Tools", "1.0.0")]
    public class AdminToolsPlugin : BaseUnityPlugin
    {
        private static BepInEx.Logging.ManualLogSource _logger;
        private GameObject windowObject;
        private ConfigEntry<KeyboardShortcut> ToggleKey { get; set; }
        private string currentTab = "Welcome";
        private List<TemplateItem> items = new List<TemplateItem>();
        private TemplateItem selectedItem;
        private const float itemHeight = 28f;  // Add at class level

        private void Awake()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource("AdminTools");

            // Initialize configuration
            ToggleKey = Config.Bind(
                "General",
                "Toggle Window",
                new KeyboardShortcut(KeyCode.End),
                "Keybind to toggle the Admin Tools window"
            );

            // Load items when the plugin starts
            // StartCoroutine(LoadItems());
            // LoadItems();
        }

        private void Update()
        {
            if (ToggleKey.Value.IsDown())
            {
                ToggleWindow();
            }
        }

        private void ToggleWindow()
        {
            if (windowObject == null)
            {
                CreateWindow();
            }
            else
            {
                windowObject.SetActive(!windowObject.activeSelf);
            }
        }

        private void CreateWindow()
        {
            // Create main window object
            windowObject = new GameObject("AdminToolsWindow");
            windowObject.transform.SetParent(transform, false);

            // Add Canvas component
            Canvas canvas = windowObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Add CanvasScaler for proper UI scaling
            CanvasScaler scaler = windowObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster for button interaction
            windowObject.AddComponent<GraphicRaycaster>();

            // Create window panel
            GameObject panel = CreatePanel();
            panel.transform.SetParent(windowObject.transform, false);

            // Create title bar (which will be draggable)
            CreateTitleBar(panel);

            // Create tabs
            CreateTabs(panel);

            // Create content area
            CreateContent(panel);
        }

        private GameObject CreatePanel()
        {
            GameObject panel = new GameObject("Panel");
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            Image image = panel.AddComponent<Image>();

            // Set panel properties
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(800, 600);

            // Set panel appearance
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            return panel;
        }

        private void CreateTitleBar(GameObject parent)
        {
            // Create title bar container
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(parent.transform, false);

            // Add title bar background
            Image titleBarImage = titleBar.AddComponent<Image>();
            titleBarImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Position the title bar
            RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, 30); // Height of title bar
            titleBarRect.anchoredPosition = Vector2.zero;

            // Add drag functionality
            DragHandler dragHandler = titleBar.AddComponent<DragHandler>();
            dragHandler.Target = parent.GetComponent<RectTransform>();

            // Create title text
            CreateTitle(titleBar);

            // Create close button
            CreateCloseButton(titleBar);
        }

        private void CreateTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);

            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "Admin Tools";
            text.color = Color.white;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Left;

            // Position the title
            RectTransform rectTransform = titleObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.offsetMin = new Vector2(10, 0);
            rectTransform.offsetMax = new Vector2(-40, 0);
        }

        private void CreateCloseButton(GameObject parent)
        {
            GameObject buttonObj = new GameObject("CloseButton");
            buttonObj.transform.SetParent(parent.transform, false);

            // Add button components
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = buttonObj.AddComponent<Button>();

            // Add hover color change
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.5f, 0.2f, 0.2f, 1f);
            colors.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            button.colors = colors;

            // Position the button
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0.5f);
            rectTransform.anchorMax = new Vector2(1, 0.5f);
            rectTransform.pivot = new Vector2(1, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-5, 0);
            rectTransform.sizeDelta = new Vector2(20, 20);

            // Add text for X
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "×";
            text.color = Color.white;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;

            // Add click handler
            button.onClick.AddListener(() => {
                windowObject.SetActive(false);
            });
        }

        // Add this new class for handling window dragging
        private class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            public RectTransform Target;
            private Vector2 dragOffset;

            public void OnBeginDrag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Target,
                    eventData.position,
                    eventData.pressEventCamera,
                    out dragOffset
                );
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Target.parent.GetComponent<RectTransform>(),
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint
                ))
                {
                    Target.localPosition = localPoint - dragOffset;
                }
            }
        }

        private void CreateTabs(GameObject parent)
        {
            GameObject tabsContainer = new GameObject("TabsContainer");
            tabsContainer.transform.SetParent(parent.transform, false);

            // Add background
            Image tabsBg = tabsContainer.AddComponent<Image>();
            tabsBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Position tabs container
            RectTransform tabsRect = tabsContainer.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0, 1);
            tabsRect.anchorMax = new Vector2(1, 1);
            tabsRect.pivot = new Vector2(0.5f, 1);
            tabsRect.sizeDelta = new Vector2(0, 30);
            tabsRect.anchoredPosition = new Vector2(0, -30); // Position below title bar

            string[] tabs = new string[] { "Items", "Weapons", "Skills", "Traders" };
            float tabWidth = 100f;
            float startX = 10f;

            for (int i = 0; i < tabs.Length; i++)
            {
                CreateTabButton(tabsContainer, tabs[i], startX + (tabWidth * i), tabWidth);
            }
        }

        private void CreateTabButton(GameObject parent, string tabName, float xPos, float width)
        {
            GameObject buttonObj = new GameObject($"Tab_{tabName}");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();

            // Position
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(xPos, 0);
            rectTransform.sizeDelta = new Vector2(width - 2, -2); // -2 for spacing

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tabName;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            button.colors = colors;

            // Click handler
            button.onClick.AddListener(() => {
                currentTab = tabName;
                UpdateContent();
            });
        }

        private void CreateContent(GameObject parent)
        {
            GameObject content = new GameObject("Content");
            content.transform.SetParent(parent.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, -60);

            // Create welcome panel
            CreateWelcomePanel(content);

            // Create split view for Items tab
            CreateItemsSplitView(content);

            // Create other tab contents
            CreatePlaceholderContent(content, "Weapons");
            CreatePlaceholderContent(content, "Skills");
            CreatePlaceholderContent(content, "Traders");

            // Set initial visibility
            foreach (Transform child in content.transform)
            {
                child.gameObject.SetActive(child.name == $"Content_{currentTab}");
            }
        }

        private void CreateWelcomePanel(GameObject parent)
        {
            GameObject welcomePanel = new GameObject("Content_Welcome");
            welcomePanel.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = welcomePanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);

            Image bg = welcomePanel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Create content container for text
            GameObject content = new GameObject("WelcomeContent");
            content.transform.SetParent(welcomePanel.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -20);

            TextMeshProUGUI text = content.AddComponent<TextMeshProUGUI>();
            text.text = "Welcome to Admin Tools\n\n" +
                        "This tool provides various administrative functions for SPT Tarkov:\n\n" +
                        "• Items Tab: Browse and manage all game items\n" +
                        "• Weapons Tab: Spawn one of your weapon builds\n" +
                        "• Skills Tab: Modify player skills\n" +
                        "• Traders Tab: Manage trader relations and inventory\n\n" +
                        "Select a tab above to get started.";
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
        }

        private void CreateItemsSplitView(GameObject parent)
        {
            // Left panel (items list)
            GameObject leftPanel = new GameObject("ItemsList");
            leftPanel.transform.SetParent(parent.transform, false);

            RectTransform leftRect = leftPanel.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0.25f, 1);
            leftRect.sizeDelta = Vector2.zero;
            leftRect.offsetMin = new Vector2(10, 10);
            leftRect.offsetMax = new Vector2(-5, -10);

            Image leftBg = leftPanel.AddComponent<Image>();
            leftBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Create search bar container
            GameObject searchContainer = new GameObject("SearchContainer");
            searchContainer.transform.SetParent(leftPanel.transform, false);

            RectTransform searchRect = searchContainer.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0, 1);
            searchRect.anchorMax = new Vector2(1, 1);
            searchRect.sizeDelta = new Vector2(0, 28);  // Match item button height
            searchRect.anchoredPosition = new Vector2(0, 21);  // Position just below tabs

            // Create search input field
            GameObject searchInput = new GameObject("SearchInput");
            searchInput.transform.SetParent(searchContainer.transform, false);

            RectTransform inputRect = searchInput.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            Image inputBg = searchInput.AddComponent<Image>();
            inputBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            TMP_InputField inputField = searchInput.AddComponent<TMP_InputField>();
            inputField.caretWidth = 4;
            inputField.customCaretColor = true;
            inputField.caretColor = Color.white;
            inputField.caretBlinkRate = 0.85f;
            inputField.richText = true;
            inputField.onFocusSelectAll = false;

            // Create input text
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(searchInput.transform, false);

            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 0);
            textAreaRect.offsetMax = new Vector2(-5, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);

            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;  // Center text

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Create placeholder with centered text
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);

            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Search items...";
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(1, 1, 1, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;  // Center placeholder text

            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            // Setup input field
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.onValueChanged.AddListener(OnSearchValueChanged);

            // Create custom caret
            GameObject caret = new GameObject("Caret");
            caret.transform.SetParent(textArea.transform, false);

            RectTransform caretRect = caret.AddComponent<RectTransform>();
            caretRect.anchorMin = new Vector2(0, 0.2f);  // Move up from bottom
            caretRect.anchorMax = new Vector2(0, 0.8f);  // Move down from top
            caretRect.sizeDelta = new Vector2(2, 0);     // Just set width, height controlled by anchors
            caretRect.anchoredPosition = Vector2.zero;

            Image caretImage = caret.AddComponent<Image>();
            caretImage.color = Color.white;

            // Add caret controller
            CaretController caretController = caret.AddComponent<CaretController>();
            caretController.inputField = inputField;

            // Create selection area
            GameObject selectionArea = new GameObject("Selection");
            selectionArea.transform.SetParent(textArea.transform, false);

            RectTransform selectionRect = selectionArea.AddComponent<RectTransform>();
            selectionRect.anchorMin = new Vector2(0, 0);
            selectionRect.anchorMax = new Vector2(0, 1);
            selectionRect.pivot = new Vector2(0, 0.5f);
            selectionRect.localPosition = Vector2.zero;
            selectionRect.sizeDelta = Vector2.zero;

            Image selectionImage = selectionArea.AddComponent<Image>();
            selectionImage.color = new Color(0.2f, 0.5f, 0.9f, 0.5f);

            // Set the selection colors in the input field
            inputField.selectionColor = new Color(0.2f, 0.5f, 0.9f, 0.5f);

            // Create ScrollView with adjusted position
            RectTransform scrollRect = leftPanel.GetComponent<RectTransform>();
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-5, -45);  // Adjusted to make room for search bar

            CreateItemsScrollView(leftPanel.gameObject);

            // Right panel setup
            GameObject rightPanel = new GameObject("ItemDetails");
            rightPanel.transform.SetParent(parent.transform, false);

            RectTransform rightRect = rightPanel.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.25f, 0);  // Match left panel
            rightRect.anchorMax = new Vector2(1, 1);
            rightRect.sizeDelta = Vector2.zero;
            rightRect.offsetMin = new Vector2(5, 10);  // Add padding
            rightRect.offsetMax = new Vector2(-10, -10);  // Add padding

            Image rightBg = rightPanel.AddComponent<Image>();
            rightBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);  // Match left panel color
        }

        private void OnSearchValueChanged(string searchText)
        {
            if (windowObject == null) return;

            Transform content = windowObject.transform.Find("Panel/Content/ItemsList/ScrollView/Viewport/ItemsContent");
            if (content == null) return;

            foreach (Transform child in content)
            {
                TextMeshProUGUI itemText = child.GetComponentInChildren<TextMeshProUGUI>();
                if (itemText != null)
                {
                    bool matches = string.IsNullOrEmpty(searchText) ||
                                  itemText.text.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
                    child.gameObject.SetActive(matches);
                }
            }
        }

        private string GetBackendUrl()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            foreach (string arg in commandLineArgs)
            {
                if (arg.Contains("BackendUrl"))
                    return Json.Deserialize<ServerConfig>(arg.Replace("-config=", string.Empty)).BackendUrl;
            }
            return "http://127.0.0.1:6969"; // fallback
        }

        private string GetSessionId()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            foreach (string arg in commandLineArgs)
            {
                if (arg.Contains("-token="))
                    return arg.Replace("-token=", string.Empty);
            }
            return string.Empty;
        }

        private IEnumerator LoadItems()
        {
            string apiUrl = GetBackendUrl();
            string sessionId = GetSessionId();

            // Show loading state
            Transform itemsList = windowObject.transform.Find("Panel/Content/ItemsList");
            if (itemsList != null)
            {
                ShowLoadingState(itemsList.gameObject);
            }

            using (var client = new Client(apiUrl, sessionId))
            {
                var request = client.GetAsync("/admin-tools/items");
                while (!request.IsCompleted)
                {
                    yield return null;
                }

                byte[] responseBytes = request.Result;
                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                var itemsResponse = JsonConvert.DeserializeObject<ItemsResponse>(responseText);

                // Load items in chunks
                const int chunkSize = 100;
                items = new List<TemplateItem>();

                for (int i = 0; i < itemsResponse.data.Count; i += chunkSize)
                {
                    int count = Math.Min(chunkSize, itemsResponse.data.Count - i);
                    items.AddRange(itemsResponse.data.GetRange(i, count));
                    yield return null;
                }

                // Remove loading state after all chunks are processed
                Transform loadingContainer = itemsList?.Find("LoadingContainer");
                if (loadingContainer != null)
                {
                    Destroy(loadingContainer.gameObject);
                }

                if (itemsResponse.data != null)
                {
                    // Filter out items without icons
                    items = itemsResponse.data.Where(item => !string.IsNullOrEmpty(item.icon)).ToList();
                    _logger.LogInfo($"Items loaded successfully: {items.Count} items");
                    UpdateItemsList();
                }
                else
                {
                    _logger.LogError("Failed to load items: Response data is null");
                }
                yield return null;
            }
        }

        private void CreatePlaceholderContent(GameObject parent, string tabName)
        {
            GameObject placeholder = new GameObject($"Content_{tabName}");
            placeholder.transform.SetParent(parent.transform, false);
            placeholder.SetActive(false);

            TextMeshProUGUI text = placeholder.AddComponent<TextMeshProUGUI>();
            text.text = $"{tabName} content coming soon...";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform rect = placeholder.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private void UpdateContent()
        {
            if (windowObject == null)
            {
                _logger.LogError("Window object is null!");
                return;
            }

            _logger.LogInfo($"Updating content for tab: {currentTab}");

            Transform contentTransform = windowObject.transform.Find("Panel/Content");
            if (contentTransform == null)
            {
                _logger.LogError("Content container not found!");
                return;
            }

            // Log all children for debugging
            _logger.LogInfo("Content children:");
            foreach (Transform child in contentTransform)
            {
                _logger.LogInfo($"- Found child: {child.name}");
            }

            // Update content visibility with proper hierarchy checks
            foreach (Transform child in contentTransform)
            {
                bool shouldBeVisible = false;

                if (currentTab == "Items")
                {
                    shouldBeVisible = (child.name == "ItemsList" || child.name == "ItemDetails");
                    _logger.LogInfo($"Setting {child.name} visibility to {shouldBeVisible}");
                }
                else
                {
                    shouldBeVisible = child.name == $"Content_{currentTab}";
                    _logger.LogInfo($"Setting {child.name} visibility to {shouldBeVisible}");
                }

                if (child.gameObject.activeSelf != shouldBeVisible)
                {
                    child.gameObject.SetActive(shouldBeVisible);
                    _logger.LogInfo($"Changed {child.name} visibility to {shouldBeVisible}");
                }
            }

            // If it's the Items tab, make sure to update the items list
            if (currentTab == "Items")
            {
                UpdateItemsList();
            }
        }

        private void UpdateItemsList()
        {
            if (windowObject == null)
            {
                _logger.LogError("Window object is null!");
                return;
            }

            Transform itemsList = windowObject.transform.Find("Panel/Content/ItemsList");
            if (itemsList == null)
            {
                _logger.LogError("ItemsList not found!");
                return;
            }

            // Create ScrollView if it doesn't exist
            Transform scrollView = itemsList.Find("ScrollView");
            if (scrollView == null)
            {
                _logger.LogInfo("Creating ScrollView structure");
                CreateItemsScrollView(itemsList.gameObject);
                return; // CreateItemsScrollView will handle initialization
            }

            // If ScrollView exists, just update the virtual scroller
            ScrollRect scroll = scrollView.GetComponent<ScrollRect>();
            if (scroll != null && items != null && items.Count > 0)
            {
                new VirtualScrollController(scroll, items, this);
            }
        }

        private void CreateItemsScrollView(GameObject parent)
        {
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent.transform, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            // Add mask
            Image maskImage = viewport.AddComponent<Image>();
            maskImage.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create content container
            GameObject itemsContent = new GameObject("ItemsContent");
            itemsContent.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = itemsContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, items.Count * itemHeight);

            // Setup scroll view references
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Only load items if they haven't been loaded yet
            if (items == null || items.Count == 0)
            {
                StartCoroutine(LoadItems());
            }
            else
            {
                // Initialize virtual scroll controller with existing items
                new VirtualScrollController(scroll, items, this);
            }
        }

        private void UpdateItemDetails(TemplateItem details)
        {
            if (details == null) return;

            Transform detailsPanel = windowObject.transform.Find("Panel/Content/ItemDetails");
            if (detailsPanel == null) return;

            // Clear existing details
            foreach (Transform child in detailsPanel)
            {
                Destroy(child.gameObject);
            }

            // Create scrollable content
            GameObject scrollView = new GameObject("DetailsScroll");
            scrollView.transform.SetParent(detailsPanel, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -5);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();

            layout.padding = new RectOffset(50, 45, 0, 0);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            // Add details sections
            CreateDetailSection(content, "Name", details.name);
            CreateDetailSection(content, "Description", details.description);
            CreateDetailSection(content, "ID", details.id);
            CreateDetailSection(content, "Price", $"{details.price:N0} ₽");

            // Add bundle preview if available
            if (!string.IsNullOrEmpty(details.bundle))
            {
                CreateBundlePreview(content, details.bundle);
            }
        }

        private void CreateBundlePreview(GameObject parent, string bundlePath)
        {
            GameObject section = new GameObject("Section_Bundle");
            section.transform.SetParent(parent.transform, false);

            RectTransform sectionRect = section.AddComponent<RectTransform>();
            sectionRect.anchorMin = Vector2.zero;
            sectionRect.anchorMax = Vector2.one;
            sectionRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();

            layout.padding = new RectOffset(50, 50, 10, 10);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create bundle preview container
            GameObject previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(section.transform, false);

            RectTransform previewRect = previewObj.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0, 0);
            previewRect.anchorMax = new Vector2(1, 1);
            previewRect.sizeDelta = Vector2.zero;

            // Add layout element for sizing
            LayoutElement previewLayout = previewObj.AddComponent<LayoutElement>();
            previewLayout.minHeight = 200;
            previewLayout.preferredHeight = 200;
            previewLayout.flexibleWidth = 1;

            // Add image component for background
            Image previewImage = previewObj.AddComponent<Image>();
            previewImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            // Load and display bundle
            if (!string.IsNullOrEmpty(bundlePath))
            {
                StartCoroutine(LoadBundle(bundlePath, previewObj));
            }
        }

        private IEnumerator LoadBundle(string bundlePath, GameObject previewObj)
        {
            string gamePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullBundlePath = Path.Combine(gamePath, "EscapeFromTarkov_Data", "StreamingAssets", "Windows", bundlePath);

            _logger.LogInfo($"Loading bundle from: {fullBundlePath}");

            var bundle = AssetBundle.LoadFromFile(fullBundlePath);
            if (bundle != null)
            {
                try
                {
                    string[] assetNames = bundle.GetAllAssetNames();
                    _logger.LogInfo($"Available assets in bundle: {string.Join(", ", assetNames)}");

                    // Look for container or simple model first
                    string assetName = assetNames.FirstOrDefault(name =>
                        name.Contains("_container.") ||
                        name.Contains("_simple.") ||
                        name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(assetName))
                    {
                        _logger.LogInfo($"Loading asset: {assetName}");

                        var asset = bundle.LoadAsset<GameObject>(assetName);
                        if (asset != null)
                        {
                            // Create preview container
                            GameObject previewContainer = new GameObject("PreviewContainer");
                            previewContainer.transform.SetParent(previewObj.transform, false);

                            RectTransform containerRect = previewContainer.AddComponent<RectTransform>();
                            containerRect.anchorMin = Vector2.zero;
                            containerRect.anchorMax = Vector2.one;
                            containerRect.sizeDelta = Vector2.zero;

                            // Create image component
                            Image itemImage = previewContainer.AddComponent<Image>();
                            itemImage.preserveAspect = true;

                            // Create render texture
                            RenderTexture renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                            renderTexture.Create();

                            // Setup camera
                            GameObject cameraObj = new GameObject("PreviewCamera");
                            Camera camera = cameraObj.AddComponent<Camera>();
                            camera.clearFlags = CameraClearFlags.SolidColor;
                            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0f);
                            camera.orthographic = true;
                            camera.orthographicSize = 0.3f;
                            camera.targetTexture = renderTexture;
                            camera.transform.position = new Vector3(0, 0.3f, -1f);

                            // Instantiate model
                            GameObject model = Instantiate(asset);
                            model.transform.position = Vector3.zero;
                            model.transform.rotation = Quaternion.Euler(0, 45, 0);

                            // Add lighting
                            GameObject lightObj = new GameObject("PreviewLight");
                            Light light = lightObj.AddComponent<Light>();
                            light.type = LightType.Directional;
                            light.intensity = 1.2f;
                            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

                            // Render to texture
                            camera.Render();

                            // Convert render texture to sprite
                            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                            RenderTexture.active = renderTexture;
                            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                            tex.Apply();

                            // Create and assign sprite
                            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            itemImage.sprite = sprite;

                            // Cleanup
                            yield return new WaitForEndOfFrame();
                            RenderTexture.active = null;
                            Destroy(model);
                            Destroy(cameraObj);
                            Destroy(lightObj);
                            renderTexture.Release();
                        }
                        else
                        {
                            _logger.LogError($"Failed to load asset: {assetName}");
                        }
                    }
                }
                finally
                {
                    bundle.Unload(false);
                }
            }
            else
            {
                _logger.LogError($"Failed to load bundle: {fullBundlePath}");
            }

            yield return null;
        }

        private void CreateDetailSection(GameObject parent, string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            GameObject section = new GameObject($"Section_{label}");
            section.transform.SetParent(parent.transform, false);

            RectTransform scrollRect = section.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();

            layout.padding = new RectOffset(50, 45, 0, 0);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(section.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.enableWordWrapping = true;

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(section.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 14;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Left;
            valueText.enableWordWrapping = true;

            // Add layout elements to control size
            LayoutElement sectionLayout = section.AddComponent<LayoutElement>();
            sectionLayout.minHeight = 50;
            sectionLayout.flexibleWidth = 1;

            LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minHeight = 20;
            labelLayout.flexibleWidth = 1;

            LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
            valueLayout.minHeight = 20;
            valueLayout.flexibleWidth = 1;
        }

        private void OnDestroy()
        {
            if (windowObject != null)
            {
                Destroy(windowObject);
                windowObject = null;
            }

            // Clean up logger
            if (_logger != null)
            {
                _logger = null;
            }

            // Clean up items list
            if (items != null)
            {
                items.Clear();
                items = null;
            }

            selectedItem = null;
        }

        private class CaretController : MonoBehaviour
        {
            public TMP_InputField inputField;
            private Image caretImage;
            private float blinkTimer;
            private RectTransform textAreaTransform;
            private RectTransform selectionAreaTransform;

            void Start()
            {
                caretImage = GetComponent<Image>();
                textAreaTransform = transform.parent as RectTransform;
                selectionAreaTransform = transform.parent.Find("Selection")?.GetComponent<RectTransform>();
            }

            void Update()
            {
                if (inputField.isFocused)
                {
                    blinkTimer += Time.deltaTime;
                    if (blinkTimer > inputField.caretBlinkRate)
                    {
                        blinkTimer = 0;
                        caretImage.enabled = !caretImage.enabled;
                    }

                    RectTransform rt = (RectTransform)transform;
                    TMP_TextInfo textInfo = inputField.textComponent.textInfo;

                    // Handle text selection
                    if (inputField.selectionStringAnchorPosition != inputField.selectionStringFocusPosition)
                    {
                        caretImage.enabled = false;
                        UpdateSelection(textInfo);
                    }
                    else
                    {
                        if (selectionAreaTransform != null)
                        {
                            selectionAreaTransform.sizeDelta = Vector2.zero;
                        }

                        if (textInfo.characterCount > 0)
                        {
                            int caretPos = inputField.caretPosition;
                            if (caretPos > 0 && caretPos <= textInfo.characterCount)
                            {
                                TMP_CharacterInfo charInfo = textInfo.characterInfo[caretPos - 1];
                                float xPos = charInfo.xAdvance;
                                Vector2 localPos = new Vector2(xPos, 0);
                                rt.localPosition = localPos;
                            }
                            else if (caretPos == 0)
                            {
                                rt.anchoredPosition = new Vector2(0, 0);
                            }
                        }
                        else
                        {
                            rt.anchoredPosition = new Vector2(0, 0);
                        }
                    }
                }
                else
                {
                    caretImage.enabled = false;
                    if (selectionAreaTransform != null)
                    {
                        selectionAreaTransform.sizeDelta = Vector2.zero;
                    }
                }
            }

            private void UpdateSelection(TMP_TextInfo textInfo)
            {
                if (selectionAreaTransform == null || textInfo.characterCount == 0)
                {
                    selectionAreaTransform.sizeDelta = Vector2.zero;
                    return;
                }

                int startPos = Mathf.Min(inputField.selectionStringAnchorPosition, inputField.selectionStringFocusPosition);
                int endPos = Mathf.Max(inputField.selectionStringAnchorPosition, inputField.selectionStringFocusPosition);

                if (startPos >= textInfo.characterCount || endPos >= textInfo.characterCount || startPos == endPos)
                {
                    selectionAreaTransform.sizeDelta = Vector2.zero;
                    return;
                }

                TMP_CharacterInfo startChar = textInfo.characterInfo[startPos];
                TMP_CharacterInfo endChar = textInfo.characterInfo[endPos];

                float startX = startChar.origin;
                float endX = endChar.xAdvance;
                float width = endX - startX;

                if (width > 0)
                {
                    selectionAreaTransform.localPosition = new Vector2(startX, 0);
                    selectionAreaTransform.sizeDelta = new Vector2(width, 0);
                }
                else
                {
                    selectionAreaTransform.sizeDelta = Vector2.zero;
                }
            }
        }

        private class ItemsResponse
        {
            public List<TemplateItem> data { get; set; }
        }

        private class ServerConfig
        {
            public string BackendUrl { get; set; }
        }

        private void ShowLoadingState(GameObject parent)
        {
            // Create loading container
            GameObject loadingContainer = new GameObject("LoadingContainer");
            loadingContainer.transform.SetParent(parent.transform, false);

            RectTransform loadingRect = loadingContainer.AddComponent<RectTransform>();
            loadingRect.anchorMin = Vector2.zero;
            loadingRect.anchorMax = Vector2.one;
            loadingRect.sizeDelta = Vector2.zero;
            loadingRect.offsetMin = new Vector2(10, 10); // Add padding
            loadingRect.offsetMax = new Vector2(-10, -10);

            // Create loading text
            GameObject loadingText = new GameObject("LoadingText");
            loadingText.transform.SetParent(loadingContainer.transform, false);

            TextMeshProUGUI text = loadingText.AddComponent<TextMeshProUGUI>();
            text.text = "Loading items...\n\nPlease wait while the items are loading.\n\nIt may take a while because of the sheer number of items.";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = loadingText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 40); // Add padding and space for spinner
            textRect.offsetMax = new Vector2(-10, -10);

            // Create spinning circle
            GameObject spinner = new GameObject("Spinner");
            spinner.transform.SetParent(loadingContainer.transform, false);

            RectTransform spinnerRect = spinner.AddComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 1f);
            spinnerRect.anchorMax = new Vector2(0.5f, 1f);
            spinnerRect.sizeDelta = new Vector2(30, 30);
            spinnerRect.anchoredPosition = new Vector2(0, -20);

            Image spinnerImage = spinner.AddComponent<Image>();
            spinnerImage.color = Color.white;

            // Add rotation animation component
            spinner.AddComponent<SpinnerAnimation>();
        }

        private class SpinnerAnimation : MonoBehaviour
        {
            private RectTransform rectTransform;
            private float rotationSpeed = 270f; // degrees per second

            private void Start()
            {
                rectTransform = GetComponent<RectTransform>();
            }

            private void Update()
            {
                rectTransform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            }
        }

        private class VirtualScrollController
        {
            private readonly AdminToolsPlugin parent;
            private ScrollRect scrollRect;
            private RectTransform content;
            private float itemHeight = 28f;
            private float viewportHeight;
            private List<GameObject> pooledItems = new List<GameObject>();
            private List<TemplateItem> items;
            private int firstVisibleIndex = -1;
            private int lastVisibleIndex = -1;
            private const int BUFFER_ITEMS = 5;
            private string selectedItemId;

            public VirtualScrollController(ScrollRect scroll, List<TemplateItem> itemsList, AdminToolsPlugin parentPlugin)
            {
                parent = parentPlugin;
                scrollRect = scroll;
                content = scroll.content;
                items = itemsList;

                // Remove layout group and size fitter from content
                if (content.GetComponent<LayoutGroup>() != null)
                    UnityEngine.Object.Destroy(content.GetComponent<LayoutGroup>());
                if (content.GetComponent<ContentSizeFitter>() != null)
                    UnityEngine.Object.Destroy(content.GetComponent<ContentSizeFitter>());

                // Set initial sizes
                viewportHeight = ((RectTransform)scroll.viewport).rect.height;
                float totalHeight = items.Count * itemHeight;
                content.sizeDelta = new Vector2(0, totalHeight);

                scrollRect.onValueChanged.AddListener(OnScroll);
                CreateItemPool();
                UpdateVisibleItems();
            }

            private void CreateItemPool()
            {
                int maxVisibleItems = Mathf.CeilToInt(viewportHeight / itemHeight) + (BUFFER_ITEMS * 2);
                for (int i = 0; i < maxVisibleItems; i++)
                {
                    GameObject item = CreatePooledItem();
                    pooledItems.Add(item);
                    item.SetActive(false);
                }
            }

            private GameObject CreatePooledItem()
            {
                GameObject itemButton = new GameObject("PooledItem");
                itemButton.transform.SetParent(content, false);

                RectTransform rectTransform = itemButton.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.sizeDelta = new Vector2(-10, itemHeight - 2);
                rectTransform.pivot = new Vector2(0.5f, 1);

                // Background
                GameObject background = new GameObject("Background");
                background.transform.SetParent(itemButton.transform, false);

                RectTransform bgRect = background.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.offsetMin = new Vector2(5, 1);
                bgRect.offsetMax = new Vector2(-5, -1);

                Image buttonBg = background.AddComponent<Image>();
                buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                // Icon
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(itemButton.transform, false);

                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.pivot = new Vector2(0, 0.5f);
                iconRect.sizeDelta = new Vector2(16, 16);
                iconRect.anchoredPosition = new Vector2(10, 0);

                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.preserveAspect = true;

                // Text with adjusted position for icon
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(itemButton.transform, false);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.offsetMin = new Vector2(32, 0); // Make room for icon
                textRect.offsetMax = new Vector2(-5, 0);

                TextMeshProUGUI itemText = textObj.AddComponent<TextMeshProUGUI>();
                itemText.fontSize = 13;
                itemText.color = Color.white;
                itemText.alignment = TextAlignmentOptions.Left;
                itemText.enableWordWrapping = false;
                itemText.overflowMode = TextOverflowModes.Truncate;

                Button button = itemButton.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.targetGraphic = buttonBg;

                return itemButton;
            }

            private void OnScroll(Vector2 value)
            {
                UpdateVisibleItems();
            }

            private void UpdateVisibleItems()
            {
                if (items == null || items.Count == 0) return;

                // Calculate visible range based on normalized scroll position
                float normalizedPos = scrollRect.verticalNormalizedPosition;
                float contentHeight = items.Count * itemHeight;
                float scrollableHeight = contentHeight - viewportHeight;
                float scrollPos = (1 - normalizedPos) * scrollableHeight;

                int newFirstVisible = Mathf.Max(0, Mathf.FloorToInt(scrollPos / itemHeight) - BUFFER_ITEMS);
                int newLastVisible = Mathf.Min(items.Count - 1,
                    newFirstVisible + Mathf.CeilToInt(viewportHeight / itemHeight) + BUFFER_ITEMS);

                if (newFirstVisible != firstVisibleIndex || newLastVisible != lastVisibleIndex)
                {
                    foreach (var item in pooledItems)
                    {
                        item.SetActive(false);
                    }

                    int poolIndex = 0;
                    for (int i = newFirstVisible; i <= newLastVisible && poolIndex < pooledItems.Count; i++)
                    {
                        var pooledItem = pooledItems[poolIndex];
                        UpdatePooledItem(pooledItem, items[i], i);
                        pooledItem.SetActive(true);
                        poolIndex++;
                    }

                    firstVisibleIndex = newFirstVisible;
                    lastVisibleIndex = newLastVisible;
                }
            }

            private void UpdatePooledItem(GameObject pooledItem, TemplateItem item, int index)
            {
                RectTransform rect = pooledItem.GetComponent<RectTransform>();
                float yPos = -(index * itemHeight);
                rect.anchoredPosition = new Vector2(0, yPos);

                var text = pooledItem.GetComponentInChildren<TextMeshProUGUI>();
                text.text = item.name;

                // Set item ID
                var itemData = pooledItem.GetComponent<ItemData>();
                if (itemData == null)
                    itemData = pooledItem.AddComponent<ItemData>();
                itemData.id = item.id;

                // Update icon
                var iconImage = pooledItem.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage != null && !string.IsNullOrEmpty(item.icon))
                {
                    parent.StartCoroutine(LoadIcon(item.icon, iconImage));
                }

                var button = pooledItem.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnItemSelected(item));

                // Set background color based on selection
                var background = pooledItem.transform.Find("Background");
                if (background != null)
                {
                    var buttonBg = background.GetComponent<Image>();
                    buttonBg.color = (item.id == selectedItemId)
                        ? new Color(0.3f, 0.5f, 0.9f, 1f)
                        : new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            }

            private void OnItemSelected(TemplateItem item)
            {
                selectedItemId = item.id;

                foreach (var pooledItem in pooledItems)
                {
                    var background = pooledItem.transform.Find("Background");
                    if (background != null)
                    {
                        var buttonBg = background.GetComponent<Image>();
                        var itemData = pooledItem.GetComponent<ItemData>();
                        buttonBg.color = (itemData != null && itemData.id == selectedItemId)
                            ? new Color(0.3f, 0.5f, 0.9f, 1f)
                            : new Color(0.2f, 0.2f, 0.2f, 1f);
                    }
                }

                parent.selectedItem = item;
                parent.StartCoroutine(UpdateItemDetailsAsync(item.id));
            }

            private IEnumerator UpdateItemDetailsAsync(string itemId)
            {
                string apiUrl = parent.GetBackendUrl();
                string sessionId = parent.GetSessionId();

                using (var client = new Client(apiUrl, sessionId))
                {
                    var jsonContent = JsonConvert.SerializeObject(new { id = itemId });
                    var request = client.PostAsync("/admin-tools/items/info", Encoding.UTF8.GetBytes(jsonContent));
                    while (!request.IsCompleted)
                    {
                        yield return null;
                    }

                    byte[] responseBytes = request.Result;
                    string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                    var itemResponse = JsonConvert.DeserializeObject<ItemInfoResponse>(responseText);

                    if (itemResponse?.data != null)
                    {
                        parent.UpdateItemDetails(itemResponse.data);
                    }
                    else
                    {
                        _logger.LogError($"Failed to get item info for {itemId}: Response data is null");
                    }
                }
            }

            private IEnumerator LoadIcon(string iconPath, Image iconImage)
            {
                // Remove "/files/" from the beginning if present
                iconPath = iconPath.Replace("/files/", "");

                // Extract item type from icon path
                EItemType itemType = EItemType.None;

                // Handle weapon mods with incomplete paths
                if (iconPath == "handbook/")
                {
                    itemType = EItemType.Mod;
                }
                else if (iconPath.Contains("icon_weapons"))
                {
                    itemType = EItemType.Weapon;
                    if (iconPath.Contains("_melee"))
                        itemType = EItemType.Knife;
                }
                else if (iconPath.Contains("icon_ammo"))
                {
                    itemType = EItemType.Ammo;
                }
                else if (iconPath.Contains("icon_medical"))
                {
                    itemType = EItemType.Meds;
                }
                else if (iconPath.Contains("icon_money") || iconPath.Contains("icon_barter"))
                {
                    itemType = EItemType.Barter;
                }
                else if (iconPath.Contains("icon_maps"))
                {
                    itemType = EItemType.Info;
                }
                else if (iconPath.Contains("icon_gear"))
                {
                    if (iconPath.Contains("_backpacks"))
                        itemType = EItemType.Backpack;
                    else if (iconPath.Contains("_armor"))
                        itemType = EItemType.Armor;
                    else if (iconPath.Contains("_rigs"))
                        itemType = EItemType.Rig;
                    else if (iconPath.Contains("_cases"))
                        itemType = EItemType.Container;
                    else if (iconPath.Contains("_headwear") || iconPath.Contains("_visors"))
                        itemType = EItemType.Equipment;
                    else if (iconPath.Contains("_goggles") || iconPath.Contains("_facecovers"))
                        itemType = EItemType.Goggles;
                    else
                        itemType = EItemType.Equipment;
                }
                else if (iconPath.Contains("icon_mod_") || iconPath.Contains("icon_mods"))
                {
                    itemType = EItemType.Mod;
                    if (iconPath.Contains("_magazine"))
                        itemType = EItemType.Magazine;
                }
                else if (iconPath.Contains("icon_keys"))
                {
                    itemType = EItemType.Keys;
                }
                else if (iconPath.Contains("icon_provisions"))
                {
                    itemType = EItemType.Food;
                }
                else if (iconPath.Contains("icon_quest"))
                {
                    itemType = EItemType.Special;
                }
                else if (iconPath.Contains("icon_spec"))
                {
                    itemType = EItemType.Special;
                }

                // Load icon from game files
                var sprite = EFTHardSettings.Instance.StaticIcons.GetItemTypeIcon(itemType);
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                }
                yield return null;
            }
        }

        private IEnumerator GetItemInfo(string itemId)
        {
            string apiUrl = GetBackendUrl();
            string sessionId = GetSessionId();

            using (var client = new Client(apiUrl, sessionId))
            {
                var jsonContent = JsonConvert.SerializeObject(new { id = itemId });
                var request = client.PostAsync("/admin-tools/items/info", Encoding.UTF8.GetBytes(jsonContent));
                while (!request.IsCompleted)
                {
                    yield return null;
                }

                byte[] responseBytes = request.Result;
                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                var itemResponse = JsonConvert.DeserializeObject<ItemInfoResponse>(responseText);

                if (itemResponse?.data != null)
                {
                    UpdateItemDetails(itemResponse.data);
                }
                else
                {
                    _logger.LogError($"Failed to get item info for {itemId}: Response data is null");
                }
            }
        }

        // Add this class to handle model rotation
        private class ModelRotator : MonoBehaviour
        {
            private float rotationSpeed = 30f;

            private void Update()
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
