using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
public class UserSettingSampleUI : MonoBehaviour
{
    [Header("연결 (비우면 실행 시 자동 생성)")]
    public UserSetting userSetting;
    public TMP_InputField rawTextInput;
    public TMP_InputField resultTextOutput;
    public Button parseButton;

    [Header("샘플 UI 자동 생성")]
    [Tooltip("체크 시 Play 진입 시 레퍼런스가 비어 있으면 Canvas·버튼·입력란을 만듭니다.")]
    public bool buildSampleAtRuntime = true;

    [TextArea(4, 12)]
    [Tooltip("파싱 테스트용 기본 텍스트")]
    public string sampleRawText =
        "이름: 홍길동\n" +
        "고객번호: A-1001\n" +
        "청구기간: 2025.01.01 - 2025.01.31\n" +
        "상품A  10,000원\n" +
        "상품B  5,500원\n" +
        "총액: 15,500원\n";

    void Awake()
    {
        if (buildSampleAtRuntime && (rawTextInput == null || resultTextOutput == null || parseButton == null))
        {
            BuildSampleUi();
        }

        if (userSetting == null)
        {
            userSetting = FindFirstObjectByType<UserSetting>();
        }

        if (userSetting == null)
        {
            userSetting = gameObject.AddComponent<UserSetting>();
        }

        if (parseButton != null)
        {
            parseButton.onClick.RemoveListener(OnParseClicked);
            parseButton.onClick.AddListener(OnParseClicked);
        }

        if (rawTextInput != null && !string.IsNullOrEmpty(sampleRawText))
        {
            rawTextInput.text = sampleRawText;
        }
    }

    public void OnParseClicked()
    {
        if (userSetting == null)
        {
            Debug.LogError("UserSettingSampleUI: UserSetting 이 없습니다.");
            return;
        }

        if (rawTextInput == null || resultTextOutput == null)
        {
            Debug.LogError("UserSettingSampleUI: InputField 연결이 없습니다.");
            return;
        }

        if (!userSetting.TryParse(rawTextInput.text, out UserSetting.ReportInfo report, out string error))
        {
            resultTextOutput.text = "파싱 실패\n" + error;
            return;
        }

        resultTextOutput.text = MakeReport.BuildReportText(report);
    }

    void BuildSampleUi()
    {
        EnsureEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        Transform root = transform;
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(root, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        StretchFull(panelRt, 40f, 40f, 40f, 40f);

        VerticalLayoutGroup vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 24, 24);
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        LayoutElement titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.minHeight = 36f;
        titleLe.preferredHeight = 36f;
        TMPro.TMP_Text titleText = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.fontSize = 22;
        titleText.color = Color.white;
        titleText.alignment = TMPro.TextAlignmentOptions.Left;
        titleText.text = "UserSetting 파싱 샘플";

        rawTextInput = CreateMultilineInputField(panelGo.transform, "RawText", "원본 텍스트 붙여넣기", font, 220f, false);
        resultTextOutput = CreateMultilineInputField(panelGo.transform, "ResultText", "(파싱 결과)", font, 280f, true);

        parseButton = CreateButton(panelGo.transform, "ParseButton", "파싱 실행", font);
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static TMP_InputField CreateMultilineInputField(Transform parent, string name, string placeholder, Font font, float preferredHeight, bool readOnly)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = preferredHeight;
        rowLe.preferredHeight = preferredHeight;
        rowLe.flexibleHeight = readOnly ? 1f : 0f;

        Image rowImage = row.AddComponent<Image>();
        rowImage.color = readOnly ? new Color(0.2f, 0.2f, 0.22f, 1f) : new Color(0.95f, 0.95f, 0.97f, 1f);
        rowImage.raycastTarget = true;
        RectTransform rowRt = row.GetComponent<RectTransform>();
        StretchFull(rowRt);

        TMP_InputField field = row.AddComponent<TMP_InputField>();
        field.targetGraphic = rowImage;
        field.lineType = TMP_InputField.LineType.MultiLineNewline;

        GameObject phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(row.transform, false);
        TMPro.TMP_Text phText = phGo.AddComponent<TMPro.TextMeshProUGUI>();
        phText.fontSize = 14;
        phText.color = new Color(0.4f, 0.4f, 0.45f);
        phText.text = placeholder;
        phText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        phText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        RectTransform phRt = phGo.GetComponent<RectTransform>();
        StretchWithPadding(phRt, 10f, 10f, 8f, 8f);
        field.placeholder = phText;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(row.transform, false);
        TMPro.TMP_Text text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = readOnly ? new Color(0.85f, 0.88f, 0.9f) : new Color(0.1f, 0.1f, 0.12f);
        text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        text.alignment = TMPro.TextAlignmentOptions.TopLeft;
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        StretchWithPadding(textRt, 10f, 10f, 8f, 8f);
        field.textComponent = text;
        field.text = string.Empty;
        field.readOnly = readOnly;

        return field;
    }

    static Button CreateButton(Transform parent, string name, string label, Font font)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 48f;
        rowLe.preferredHeight = 48f;

        Image img = row.AddComponent<Image>();
        img.color = new Color(0.25f, 0.45f, 0.85f, 1f);
        Button btn = row.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.95f);
        colors.pressedColor = new Color(0.2f, 0.35f, 0.7f);
        btn.colors = colors;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(row.transform, false);
        TMPro.TMP_Text text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.text = label;
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        StretchFull(textRt);

        return btn;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchWithPadding(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void StretchFull(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
