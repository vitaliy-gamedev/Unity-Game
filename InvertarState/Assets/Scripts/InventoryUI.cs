using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private const int InventoryColumns = 9;
    private const int InventoryRows = 3;
    private const int CraftingColumns = 2;
    private const int CraftingRows = 2;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 6f;

    private InventorySystem _inventorySystem;

    private GameObject _canvasObject;
    private GameObject _background;
    private GameObject _inventoryPanel;
    private GameObject _cursorObject;
    private GameObject _splitPanel;

    private InventorySlotUI[] _inventorySlotUI;
    private InventorySlotUI[] _craftingSlotUI;
    private InventorySlotUI _resultSlotUI;

    private Text[] _inventoryItemNames;
    private Text[] _inventoryAmounts;

    private Text[] _craftingItemNames;
    private Text[] _craftingAmounts;

    private Text _resultItemName;
    private Text _resultAmount;

    private Text _cursorItemName;
    private Text _cursorAmount;

    private InputField _splitInput;

    private InventorySlotUI _splitSource;

    private bool _isOpen;
    private InventorySlotUI _dragSource;
    private bool _wasDragging;
    private bool _isInitialized;

    private bool _previousCursorVisible;
    private CursorLockMode _previousCursorLockState;
    private Coroutine _resetDragFlagRoutine;

    public bool IsOpen => _isOpen;

    public void Initialize(InventorySystem inventorySystem)
    {
        if (inventorySystem == null)
        {
            return;
        }

        _inventorySystem = inventorySystem;

        CreateCanvas();
        CreateInventoryWindow();
        CreateCursorUI();
        CreateSplitWindow();

        Refresh();
        SetOpen(false);

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SetOpen(!_isOpen);
        }

        if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_splitPanel != null && _splitPanel.activeSelf)
            {
                CloseSplitWindow();
            }
            else
            {
                SetOpen(false);
            }
        }

        if (_isOpen)
        {
            KeepCursorUnlocked();
        }

        UpdateCursorUI();
    }

    private void OnApplicationQuit()
    {
        if (_inventorySystem != null)
        {
            _inventorySystem.ReturnCursorToInventory();
        }
    }

    public void HandleSlotClick(
        InventorySlotUI slot,
        PointerEventData.InputButton button,
        bool exactSplit)
    {
        if (_wasDragging)
        {
            _wasDragging = false;
            return;
        }

        if (exactSplit)
        {
            OpenSplitWindow(slot);
            return;
        }

        _inventorySystem.HandleSlotClick(slot.Area, slot.Index, button, false);
        Refresh();
    }

    public void BeginDrag(InventorySlotUI slot)
    {
        if (slot.Area == InventorySlotArea.Result)
        {
            return;
        }

        InventorySlotData data = _inventorySystem.GetSlot(slot.Area, slot.Index);

        if (data == null || data.IsEmpty)
        {
            return;
        }

        _dragSource = slot;
        _wasDragging = false;
    }

    public void Drag(InventorySlotUI slot, PointerEventData eventData)
    {
        if (_dragSource == null)
        {
            return;
        }

        if (eventData.delta.sqrMagnitude > 0.01f)
        {
            _wasDragging = true;
        }
    }

    public void EndDrag(InventorySlotUI slot)
    {
        _dragSource = null;

        // _wasDragging потрібен лише для того, щоб приглушити "примарний"
        // OnPointerClick, який InputSystemUIInputModule іноді все ж викликає
        // одразу після drop у той самий слот. Якщо клік не прийшов (наприклад,
        // предмет перетягнули в ІНШИЙ слот - OnPointerClick там взагалі не
        // спрацьовує), прапорець раніше залишався true назавжди і зіпсовував
        // наступний звичайний клік по будь-якому слоту. Скидаємо його на
        // наступному кадрі, якщо цього не зробив клік.
        if (_resetDragFlagRoutine != null)
        {
            StopCoroutine(_resetDragFlagRoutine);
        }

        _resetDragFlagRoutine = StartCoroutine(ResetWasDraggingNextFrame());
    }

    private IEnumerator ResetWasDraggingNextFrame()
    {
        yield return null;
        _wasDragging = false;
        _resetDragFlagRoutine = null;
    }

    public void Drop(InventorySlotUI source, InventorySlotUI target)
    {
        if (source == target)
        {
            return;
        }

        if (target.Area == InventorySlotArea.Result)
        {
            return;
        }

        _inventorySystem.TransferSlot(
            source.Area,
            source.Index,
            target.Area,
            target.Index);

        Refresh();
    }

    private void CreateCanvas()
    {
        _canvasObject = new GameObject("InventoryCanvas");

        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _canvasObject.AddComponent<GraphicRaycaster>();

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule standaloneInputModule =
            eventSystem.GetComponent<StandaloneInputModule>();

        if (standaloneInputModule != null)
        {
            // Destroy() спрацьовує лише наприкінці кадру, тож без enabled = false
            // на об'єкті якийсь час одночасно "живуть" два input-модулі, і Unity
            // видає попередження/помилку "Two input modules detected".
            standaloneInputModule.enabled = false;
            Destroy(standaloneInputModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void CreateInventoryWindow()
    {
        _background = CreateImage(
            "Background",
            _canvasObject.transform,
            new Color(0f, 0f, 0f, 0.65f));

        SetRect(_background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero);

        _inventoryPanel = CreateImage(
            "InventoryPanel",
            _canvasObject.transform,
            new Color(0.08f, 0.08f, 0.08f, 0.98f));

        RectTransform panelRect = _inventoryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 600f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText(
            "InventoryTitle",
            _inventoryPanel.transform,
            "ІНВЕНТАР",
            28,
            TextAnchor.MiddleCenter,
            new Vector2(0f, 235f),
            new Vector2(600f, 50f));

        CreateText(
            "InventoryHint",
            _inventoryPanel.transform,
            "ЛКМ - взяти/покласти   ПКМ - половина/1   Shift + ПКМ - точна кількість",
            15,
            TextAnchor.MiddleCenter,
            new Vector2(0f, -260f),
            new Vector2(900f, 40f));

        CreateInventoryGrid();
        CreateCraftingArea();
    }

    private void CreateInventoryGrid()
    {
        GameObject gridObject = new GameObject("InventoryGrid");
        gridObject.transform.SetParent(_inventoryPanel.transform, false);

        RectTransform rect = gridObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(552f, 180f);
        rect.anchoredPosition = new Vector2(-185f, -35f);

        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = InventoryColumns;
        grid.childAlignment = TextAnchor.MiddleCenter;

        _inventorySlotUI = new InventorySlotUI[27];
        _inventoryItemNames = new Text[27];
        _inventoryAmounts = new Text[27];

        for (int i = 0; i < 27; i++)
        {
            InventorySlotUI slot = CreateSlot(
                gridObject.transform,
                InventorySlotArea.Inventory,
                i,
                out Text itemName,
                out Text amount);

            _inventorySlotUI[i] = slot;
            _inventoryItemNames[i] = itemName;
            _inventoryAmounts[i] = amount;
        }
    }

    private void CreateCraftingArea()
    {
        CreateText(
            "CraftingTitle",
            _inventoryPanel.transform,
            "КРАФТ",
            28,
            TextAnchor.MiddleCenter,
            new Vector2(280f, 205f),
            new Vector2(300f, 50f));

        GameObject gridObject = new GameObject("CraftingGrid");
        gridObject.transform.SetParent(_inventoryPanel.transform, false);

        RectTransform rect = gridObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(118f, 118f);
        rect.anchoredPosition = new Vector2(250f, 60f);

        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CraftingColumns;
        grid.childAlignment = TextAnchor.MiddleCenter;

        _craftingSlotUI = new InventorySlotUI[4];
        _craftingItemNames = new Text[4];
        _craftingAmounts = new Text[4];

        for (int i = 0; i < 4; i++)
        {
            InventorySlotUI slot = CreateSlot(
                gridObject.transform,
                InventorySlotArea.Crafting,
                i,
                out Text itemName,
                out Text amount);

            _craftingSlotUI[i] = slot;
            _craftingItemNames[i] = itemName;
            _craftingAmounts[i] = amount;
        }

        CreateText(
            "CraftArrow",
            _inventoryPanel.transform,
            "→",
            40,
            TextAnchor.MiddleCenter,
            new Vector2(350f, 60f),
            new Vector2(60f, 60f));

        GameObject resultObject = CreateImage(
            "CraftingResult",
            _inventoryPanel.transform,
            new Color(0.16f, 0.16f, 0.16f, 1f));

        RectTransform resultRect = resultObject.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.sizeDelta = new Vector2(80f, 80f);
        resultRect.anchoredPosition = new Vector2(430f, 60f);

        _resultSlotUI = resultObject.AddComponent<InventorySlotUI>();
        _resultSlotUI.Initialize(this, InventorySlotArea.Result, 0);

        _resultItemName = CreateText(
            "ResultName",
            resultObject.transform,
            "",
            15,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            new Vector2(70f, 45f));

        _resultAmount = CreateText(
            "ResultAmount",
            resultObject.transform,
            "",
            18,
            TextAnchor.LowerRight,
            new Vector2(-3f, 3f),
            new Vector2(35f, 25f));
    }

    private InventorySlotUI CreateSlot(
        Transform parent,
        InventorySlotArea area,
        int index,
        out Text itemName,
        out Text amount)
    {
        GameObject slotObject = CreateImage(
            $"Slot_{area}_{index}",
            parent,
            new Color(0.15f, 0.15f, 0.15f, 1f));

        InventorySlotUI slot = slotObject.AddComponent<InventorySlotUI>();
        slot.Initialize(this, area, index);

        itemName = CreateText(
            "ItemName",
            slotObject.transform,
            "",
            18,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            new Vector2(52f, 38f));

        amount = CreateText(
            "Amount",
            slotObject.transform,
            "",
            18,
            TextAnchor.LowerRight,
            new Vector2(-3f, 3f),
            new Vector2(38f, 24f));

        return slot;
    }

    private void CreateCursorUI()
    {
        _cursorObject = CreateImage(
            "CursorStack",
            _canvasObject.transform,
            new Color(0.1f, 0.1f, 0.1f, 0.9f));

        RectTransform rect = _cursorObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(110f, 55f);
        rect.anchoredPosition = new Vector2(20f, 20f);

        _cursorItemName = CreateText(
            "CursorItem",
            _cursorObject.transform,
            "",
            16,
            TextAnchor.MiddleLeft,
            new Vector2(8f, 0f),
            new Vector2(70f, 55f));

        _cursorAmount = CreateText(
            "CursorAmount",
            _cursorObject.transform,
            "",
            18,
            TextAnchor.MiddleRight,
            new Vector2(-5f, 0f),
            new Vector2(30f, 55f));
    }

    private void CreateSplitWindow()
    {
        _splitPanel = CreateImage(
            "SplitPanel",
            _canvasObject.transform,
            new Color(0.06f, 0.06f, 0.06f, 1f));

        RectTransform panelRect = _splitPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(350f, 220f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText(
            "SplitTitle",
            _splitPanel.transform,
            "РОЗДІЛИТИ СТЕК",
            24,
            TextAnchor.MiddleCenter,
            new Vector2(0f, 65f),
            new Vector2(300f, 45f));

        _splitInput = CreateInputField(
            _splitPanel.transform,
            new Vector2(0f, 10f),
            new Vector2(180f, 50f));

        CreateButton(
            _splitPanel.transform,
            "ВЗЯТИ",
            new Vector2(-75f, -60f),
            new Vector2(130f, 45f),
            ConfirmSplit);

        CreateButton(
            _splitPanel.transform,
            "СКАСУВАТИ",
            new Vector2(75f, -60f),
            new Vector2(130f, 45f),
            CloseSplitWindow);
    }

    private void OpenSplitWindow(InventorySlotUI slot)
    {
        if (slot.Area == InventorySlotArea.Result)
        {
            return;
        }

        InventorySlotData data = _inventorySystem.GetSlot(slot.Area, slot.Index);

        if (data == null || data.IsEmpty)
        {
            return;
        }

        if (!_inventorySystem.CursorSlot.IsEmpty)
        {
            return;
        }

        if (data.Amount <= 1)
        {
            return;
        }

        _splitSource = slot;
        _splitInput.text = Mathf.CeilToInt(data.Amount / 2f).ToString();
        _splitPanel.SetActive(true);
        _splitInput.Select();
        _splitInput.ActivateInputField();
    }

    private void ConfirmSplit()
    {
        if (_splitSource == null)
        {
            CloseSplitWindow();
            return;
        }

        if (!int.TryParse(_splitInput.text, out int amount))
        {
            return;
        }

        InventorySlotData source = _inventorySystem.GetSlot(
            _splitSource.Area,
            _splitSource.Index);

        if (source == null || source.IsEmpty)
        {
            CloseSplitWindow();
            return;
        }

        amount = Mathf.Clamp(amount, 1, source.Amount);

        if (_inventorySystem.TakeExactFromSlot(
            _splitSource.Area,
            _splitSource.Index,
            amount))
        {
            CloseSplitWindow();
            Refresh();
        }
    }

    private void CloseSplitWindow()
    {
        _splitSource = null;
        _splitPanel.SetActive(false);
    }

    private void Refresh()
    {
        for (int i = 0; i < _inventorySlotUI.Length; i++)
        {
            InventorySlotData slot = _inventorySystem.InventorySlots[i];
            RefreshSlot(_inventoryItemNames[i], _inventoryAmounts[i], slot);
        }

        for (int i = 0; i < _craftingSlotUI.Length; i++)
        {
            InventorySlotData slot = _inventorySystem.CraftingSlots[i];
            RefreshSlot(_craftingItemNames[i], _craftingAmounts[i], slot);
        }

        InventorySlotData result = _inventorySystem.GetCraftingResult();
        RefreshSlot(_resultItemName, _resultAmount, result);
    }

    private void RefreshSlot(Text itemName, Text amount, InventorySlotData slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            itemName.text = "";
            amount.text = "";
            return;
        }

        itemName.text = GetShortName(slot.Item.DisplayName);
        itemName.color = slot.Item.IconColor;
        amount.text = slot.Amount.ToString();
    }

    private void UpdateCursorUI()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_inventorySystem == null)
        {
            return;
        }

        if (_cursorItemName == null || _cursorAmount == null)
        {
            return;
        }

        InventorySlotData cursor = _inventorySystem.CursorSlot;

        if (cursor == null || cursor.IsEmpty || cursor.Item == null)
        {
            if (_cursorObject != null)
            {
                _cursorObject.SetActive(false);
            }

            _cursorItemName.text = "";
            _cursorAmount.text = "";
            return;
        }

        if (_cursorObject != null)
        {
            _cursorObject.SetActive(_isOpen);
        }

        _cursorItemName.text = GetShortName(cursor.Item.DisplayName);
        _cursorItemName.color = cursor.Item.IconColor;
        _cursorAmount.text = cursor.Amount.ToString();
    }

    private string GetShortName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return "";
        }

        if (displayName.Length <= 3)
        {
            return displayName;
        }

        return displayName.Substring(0, 3);
    }

    private Font _cachedFont;
    private bool _fontLookupDone;

    private Font GetFallbackFont()
    {
        if (_fontLookupDone)
        {
            return _cachedFont;
        }

        _fontLookupDone = true;

        // "LegacyRuntime.ttf" - актуальна назва в сучасних версіях Unity,
        // "Arial.ttf" - назва в старіших. Пробуємо обидва, щоб текст не
        // ставав невидимим через null-шрифт після оновлення Unity.
        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (_cachedFont == null)
        {
            _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (_cachedFont == null)
        {
            _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        if (_cachedFont == null)
        {
            Debug.LogWarning("InventoryUI: не вдалося знайти жоден системний шрифт, текст інвентарю може бути невидимим.");
        }

        return _cachedFont;
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject objectInstance = new GameObject(objectName);
        objectInstance.transform.SetParent(parent, false);

        Image image = objectInstance.AddComponent<Image>();
        image.color = color;

        return objectInstance;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        string text,
        int fontSize,
        TextAnchor alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject objectInstance = new GameObject(objectName);
        objectInstance.transform.SetParent(parent, false);

        RectTransform rect = objectInstance.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text textComponent = objectInstance.AddComponent<Text>();
        textComponent.text = GetUiText(objectName, text);
        textComponent.font = GetFallbackFont();
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;

        return textComponent;
    }

    private InputField CreateInputField(
        Transform parent,
        Vector2 position,
        Vector2 size)
    {
        GameObject objectInstance = CreateImage(
            "SplitInput",
            parent,
            new Color(0.18f, 0.18f, 0.18f, 1f));

        RectTransform rect = objectInstance.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = CreateText(
            "Text",
            objectInstance.transform,
            "",
            22,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            size - new Vector2(10f, 10f));

        InputField inputField = objectInstance.AddComponent<InputField>();
        inputField.textComponent = text;
        inputField.contentType = InputField.ContentType.IntegerNumber;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.characterLimit = 3;

        return inputField;
    }

    private void CreateButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        label = GetButtonLabel(label, action);

        GameObject objectInstance = CreateImage(
            label,
            parent,
            new Color(0.18f, 0.18f, 0.18f, 1f));

        RectTransform rect = objectInstance.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Button button = objectInstance.AddComponent<Button>();
        button.onClick.AddListener(action);

        CreateText(
            "Label",
            objectInstance.transform,
            label,
            16,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            size);
    }

    private void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = position;
    }

    private string GetUiText(string objectName, string fallback)
    {
        return objectName switch
        {
            "InventoryTitle" => "ІНВЕНТАР",
            "InventoryHint" => "ЛКМ - взяти/покласти   ПКМ - половина/1   Shift + ПКМ - точна кількість",
            "CraftingTitle" => "КРАФТ",
            "CraftArrow" => "->",
            "SplitTitle" => "РОЗДІЛИТИ СТЕК",
            _ => fallback
        };
    }

    private string GetButtonLabel(string fallback, UnityEngine.Events.UnityAction action)
    {
        if (action != null && action.Method.Name == nameof(ConfirmSplit))
        {
            return "ВЗЯТИ";
        }

        if (action != null && action.Method.Name == nameof(CloseSplitWindow))
        {
            return "СКАСУВАТИ";
        }

        return fallback;
    }

    private void SetOpen(bool value)
    {
        if (!value && _inventorySystem != null && !_inventorySystem.ReturnCursorToInventory())
        {
            Refresh();
            UpdateCursorUI();
            return;
        }

        _isOpen = value;

        if (_background != null)
        {
            _background.SetActive(value);
        }

        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(value);
        }

        if (_cursorObject != null)
        {
            _cursorObject.SetActive(value && !_inventorySystem.CursorSlot.IsEmpty);
        }

        if (!value && _splitPanel != null)
        {
            CloseSplitWindow();
        }

        if (value)
        {
            _previousCursorVisible = Cursor.visible;
            _previousCursorLockState = Cursor.lockState;

            KeepCursorUnlocked();
            Refresh();
        }
        else
        {
            UpdateCursorUI();

            // Повертаємо курсор миші до того стану, який був до відкриття
            // інвентарю (наприклад, заблокований і прихований під час гри),
            // а не завжди залишаємо його видимим.
            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockState;
        }
    }

    private void KeepCursorUnlocked()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
