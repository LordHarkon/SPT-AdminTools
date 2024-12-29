﻿using BepInEx;
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

namespace AdminTools
{
    [BepInPlugin("com.spt.admintools", "Admin Tools", "1.0.0")]
    public class AdminToolsPlugin : BaseUnityPlugin
    {
        private static BepInEx.Logging.ManualLogSource _logger;
        private GameObject windowObject;
        private ConfigEntry<KeyboardShortcut> ToggleKey { get; set; }
        private string currentTab = "Items";
        private List<TemplateItem> items = new List<TemplateItem>();
        private TemplateItem selectedItem;

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
            if (windowObject != null)
            {
                Destroy(windowObject);
                windowObject = null;
            }
            else
            {
                CreateWindow();
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
                Destroy(windowObject);
                windowObject = null;
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

            // Create split view for Items tab
            CreateItemsSplitView(content);

            // Create other tab contents
            CreatePlaceholderContent(content, "Weapons");
            CreatePlaceholderContent(content, "Skills");
            CreatePlaceholderContent(content, "Traders");

            // Set initial visibility
            foreach (Transform child in content.transform)
            {
                bool isVisible = false;
                if (currentTab == "Items")
                {
                    isVisible = (child.name == "ItemsList" || child.name == "ItemDetails");
                }
                else
                {
                    isVisible = child.name == $"Content_{currentTab}";
                }
                child.gameObject.SetActive(isVisible);
            }
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

                UpdateItemsList();
                _logger.LogInfo($"Items loaded successfully: {items.Count} items");
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

            // Log the full hierarchy for debugging
            _logger.LogInfo("Searching for ItemsContent in hierarchy:");
            Transform current = windowObject.transform;
            while (current != null)
            {
                _logger.LogInfo($"- {current.name}");
                current = current.parent;
            }

            // Try to find ItemsList first
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
            }

            Transform content = itemsList.Find("ScrollView/Viewport/ItemsContent");
            if (content == null)
            {
                _logger.LogError("ItemsContent still not found after creation!");
                return;
            }

            // Clear existing items
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            foreach (TemplateItem item in items)
            {
                GameObject itemButton = new GameObject(item.id);
                itemButton.transform.SetParent(content, false);

                RectTransform rectTransform = itemButton.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.sizeDelta = new Vector2(-16, 28);  // Added horizontal padding
                rectTransform.anchoredPosition = new Vector2(8, 0);  // Center the button

                LayoutElement layoutElement = itemButton.AddComponent<LayoutElement>();
                layoutElement.minHeight = 28;
                layoutElement.flexibleWidth = 1;
                layoutElement.minWidth = -1;  // Allow the layout to handle width
                layoutElement.preferredWidth = -1;  // Allow the layout to handle width

                Image buttonBg = itemButton.AddComponent<Image>();
                buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                Button button = itemButton.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.pressedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                colors.selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.fadeDuration = 0.1f;
                button.colors = colors;

                // Create text container
                GameObject textContainer = new GameObject("Text");
                textContainer.transform.SetParent(itemButton.transform, false);

                RectTransform textRect = textContainer.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.sizeDelta = new Vector2(-24, 0);
                textRect.anchoredPosition = Vector2.zero;

                TextMeshProUGUI itemText = textContainer.AddComponent<TextMeshProUGUI>();
                itemText.text = item.name;
                itemText.fontSize = 13;
                itemText.alignment = TextAlignmentOptions.Left;
                itemText.enableWordWrapping = false;
                itemText.overflowMode = TextOverflowModes.Ellipsis;
                itemText.margin = new Vector4(40, 0, 0, 0);
                itemText.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

                // Button click handler
                button.onClick.AddListener(() => {
                    selectedItem = item;

                    // Update visual selection
                    foreach (Transform child in content)
                    {
                        child.GetComponent<Image>().color =
                            child.name == item.id ?
                            new Color(0.3f, 0.3f, 0.32f, 1f) :
                            new Color(0.2f, 0.2f, 0.22f, 0.8f);
                    }

                    UpdateItemDetails();
                });
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
            contentRect.anchoredPosition = Vector2.zero;

            // Add layout components
            VerticalLayoutGroup layout = itemsContent.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter sizeFitter = itemsContent.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Setup scroll view references
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Force immediate layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);

            // Only load items if they haven't been loaded yet
            if (items == null || items.Count == 0)
            {
                StartCoroutine(LoadItems());
            }
            else
            {
                UpdateItemsList();
            }
        }

        private void UpdateItemDetails()
        {
            if (selectedItem == null) return;

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
            scrollRect.offsetMin = new Vector2(5, 5);  // Left, Bottom padding
            scrollRect.offsetMax = new Vector2(-5, -5); // Right, Top padding

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

            layout.padding = new RectOffset(5, 5, 5, 5);
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
            CreateDetailSection(content, "Name", selectedItem.name);
            CreateDetailSection(content, "Description", selectedItem.description);
            CreateDetailSection(content, "ID", selectedItem.id);
            CreateDetailSection(content, "Price", $"{selectedItem.price:N0} ₽");
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
    }
}