using S1API.PhoneApp;
using S1API.Rendering;
using S1API.UI;
using S1API_CalculatorPhoneApp.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace S1API_CalculatorPhoneApp.PhoneApps.Calculator;

/// <summary>S1API discovers and creates this app when the phone home screen starts.</summary>
public class CalculatorApp : PhoneApp
{
    private readonly CalculatorSession _session = new CalculatorSession();
    private Text? _display;
    private Text? _status;
    private Sprite? _icon;
    private Texture2D? _iconTexture;
    private bool _iconLoadAttempted;    

    protected override string AppName => "S1API_CalculatorPhoneApp.Calculator";
    protected override string AppTitle => "Calculator";
    protected override string IconLabel => "Calculator";
    protected override string IconFileName => string.Empty;
    protected override EOrientation Orientation => EOrientation.Vertical;

    protected override Sprite? IconSprite
    {
        get
        {
            if (_iconLoadAttempted) return _icon;
            _iconLoadAttempted = true;
            _iconTexture = TextureUtils.LoadTextureFromResource(typeof(CalculatorApp).Assembly,
                "CalculatorPhoneApp.CalcAppLogo.png", FilterMode.Bilinear, TextureWrapMode.Clamp);
            if (_iconTexture == null) return null;
            _icon = Sprite.Create(_iconTexture, new Rect(0, 0, _iconTexture.width, _iconTexture.height),
                new Vector2(0.5f, 0.5f));
            return _icon;
        }
    }

    protected override void OnCreatedUI(GameObject container)
    {
        _session.Press("C");
        var panel = UIFactory.Panel("Calculator", container.transform,
            new Color(0.07f, 0.09f, 0.12f), fullAnchor: true);
        var title = UIFactory.Text("Title", "Calculator", panel.transform, 24,
            TextAnchor.MiddleLeft, FontStyle.Bold);
        Place(title.rectTransform, 0.05f, 0.87f, 0.95f, 0.96f);
        title.color = Color.white;
        title.raycastTarget = false;

        _display = UIFactory.Text("Display", "0", panel.transform, 30, TextAnchor.MiddleRight);
        Place(_display.rectTransform, 0.05f, 0.73f, 0.95f, 0.87f);
        _display.color = Color.white;
        _display.raycastTarget = false;
        _display.supportRichText = false;
        _display.resizeTextForBestFit = true;
        _display.resizeTextMinSize = 12;
        _display.resizeTextMaxSize = 30;
        _status = UIFactory.Text("Status", "", panel.transform, 14, TextAnchor.MiddleRight);
        Place(_status.rectTransform, 0.05f, 0.64f, 0.95f, 0.73f);
        _status.color = new Color(1f, 0.65f, 0.45f);
        _status.raycastTarget = false;
        _status.supportRichText = false;

        string[] keys = { "C", "(", ")", "/", "7", "8", "9", "*", "4", "5", "6", "-",
            "1", "2", "3", "+", "DEL", "0", ".", "=" };
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            int column = i % 4;
            int row = i / 4;
            Color color = key == "=" ? new Color(0.12f, 0.48f, 0.35f)
                : column == 3 ? new Color(0.35f, 0.28f, 0.16f) : new Color(0.18f, 0.22f, 0.28f);
            var (go, button, label) = UIFactory.ButtonWithLabel("Key" + i, key,
                panel.transform, color, 60, 50);
            Place(go.GetComponent<RectTransform>(), 0.05f + column * 0.23f,
                0.05f + (4 - row) * 0.115f, 0.26f + column * 0.23f,
                0.15f + (4 - row) * 0.115f);
            label.fontSize = 24;
            label.color = Color.white;
            label.raycastTarget = false;
            button.onClick.AddListener(UnityCallback.Create(() => Press(key)));
            // Keep default navigation: direct Navigation access is incompatible with
            // IL2CPP wrappers when this app is compiled against Mono for CrossCompat.
        }
    }

    private void Press(string key)
    {
        _session.Press(key);
        if (_display != null) _display.text = _session.Display;
        if (_status != null) _status.text = _session.Error;
    }

    // Preserve the existing evaluation entry point for callers outside the UI.
    public bool TryEvaluate(string? expression, out double result, out string error) =>
        new CalculatorEngine().TryEvaluate(expression, out result, out error);

    private static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        _display = null;
        _status = null;
        if (_icon != null) UnityEngine.Object.Destroy(_icon);
        if (_iconTexture != null) UnityEngine.Object.Destroy(_iconTexture);
        _icon = null;
        _iconTexture = null;
        _iconLoadAttempted = false;
    }
}
