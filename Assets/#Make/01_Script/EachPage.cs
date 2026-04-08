using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class EachPage : MonoBehaviour
{
    [Header("막대그래프 데이터")]
    public float[] barValues = new float[] { 12f, 28f, 9f, 35f, 18f };

    [Header("표시")]
    public Color barColor = new Color(0.35f, 0.55f, 0.95f, 1f);
    public float barSpacing = 4f;
    public float padding = 12f;

    const string ChartHostName = "BarChartHost";

    RectTransform _chartHost;
    static Sprite _cachedWhiteSprite;

    void Awake()
    {
        EnsureChartHost();
    }

    void OnEnable()
    {
        RefreshChart();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!gameObject.activeInHierarchy)
            return;
        EnsureChartHost();
        RefreshChart();
    }
#endif

    public void RefreshChart()
    {
        EnsureChartHost();
        ClearChildren();

        if (barValues == null || barValues.Length == 0)
            return;

        float max = 0f;
        for (int i = 0; i < barValues.Length; i++)
        {
            if (barValues[i] > max)
                max = barValues[i];
        }
        if (max <= 0f)
            max = 1f;

        int n = barValues.Length;
        float halfGap = barSpacing * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float v = barValues[i];
            float t = Mathf.Clamp01(v / max);

            var colGo = new GameObject("BarColumn", typeof(RectTransform));
            colGo.transform.SetParent(_chartHost, false);
            var colRt = colGo.GetComponent<RectTransform>();
            float left = (float)i / n;
            float right = (float)(i + 1) / n;
            colRt.anchorMin = new Vector2(left, 0f);
            colRt.anchorMax = new Vector2(right, 1f);
            colRt.offsetMin = new Vector2(halfGap, padding);
            colRt.offsetMax = new Vector2(-halfGap, -padding);

            var barGo = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(colGo.transform, false);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.08f, 0f);
            barRt.anchorMax = new Vector2(0.92f, Mathf.Max(0.02f, t));
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            var img = barGo.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = barColor;
            img.type = Image.Type.Simple;
        }
    }

    void EnsureChartHost()
    {
        if (_chartHost != null)
            return;

        var t = transform.Find(ChartHostName);
        if (t != null)
        {
            _chartHost = t.GetComponent<RectTransform>();
            return;
        }

        var go = new GameObject(ChartHostName, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling();
        _chartHost = go.GetComponent<RectTransform>();
        _chartHost.anchorMin = Vector2.zero;
        _chartHost.anchorMax = Vector2.one;
        _chartHost.offsetMin = Vector2.zero;
        _chartHost.offsetMax = Vector2.zero;
    }

    void ClearChildren()
    {
        if (_chartHost == null)
            return;
        for (int i = _chartHost.childCount - 1; i >= 0; i--)
        {
            var go = _chartHost.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
#endif
                Destroy(go);
        }
    }

    static Sprite GetWhiteSprite()
    {
        if (_cachedWhiteSprite != null)
            return _cachedWhiteSprite;
        var tex = Texture2D.whiteTexture;
        _cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _cachedWhiteSprite;
    }
}
