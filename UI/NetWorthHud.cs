using System;
using S1API.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API_NetWorthGUI.UI
{
    /// <summary>Owns the screen UI only; Core supplies the game's net-worth value.</summary>
    internal sealed class NetWorthHud : IDisposable
    {
        private GameObject? _root;
        private Text? _valueText;
        private Text? _revenueText;
        private Text? _expensesText;
        private Text? _profitText;
        private string? _displayedValue;

        /// <summary>Creates one non-interactive, resolution-scaled overlay after a save loads.</summary>
        public void Show()
        {
            MelonLogger.Msg("Showing NetWorthHud");
            
            if (_root != null)
                return;

            _root = new GameObject("NetWorthGuiCanvas");
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var panel = UIFactory.Panel("NetWorthPanel", _root.transform,
                new Color(0.06f, 0.08f, 0.10f, 0.82f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var rect = panel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(340f, 222f);
            panel.GetComponent<Image>().raycastTarget = false;

            var title = UIFactory.Text("Title", "NET WORTH", panel.transform, 14,
                TextAnchor.UpperLeft, FontStyle.Bold);
            PlaceText(title, 14f, -12f, 312f, 22f);
            title.color = new Color(0.75f, 0.8f, 0.85f, 1f);

            _valueText = UIFactory.Text("Value", "--", panel.transform, 28,
                TextAnchor.UpperLeft, FontStyle.Bold);
            PlaceText(_valueText, 14f, -35f, 312f, 40f);
            var sessionTitle = UIFactory.Text("SessionTitle", "SINCE SAVE LOADED", panel.transform, 12);
            PlaceText(sessionTitle, 14f, -87f, 312f, 20f);
            sessionTitle.color = title.color;
            _revenueText = CreateRow(panel.transform, "Revenue", -115f);
            _expensesText = CreateRow(panel.transform, "Expenses", -147f);
            _profitText = CreateRow(panel.transform, "Profit", -179f);
            // No GraphicRaycaster or input handlers: this display cannot capture clicks.
        }

        /// <summary>Formats dollars consistently and avoids rebuilding unchanged text.</summary>
        public void SetValue(float netWorth)
            => SetValue(netWorth, 0m, 0m, 0m, false);

        /// <summary>Displays session cash flow supplied by Core; never calculates it from net worth.</summary>
        public void SetValue(float netWorth, decimal revenue, decimal expenses, decimal profit, bool trackingAvailable)
        {
            if (_valueText == null)
                return;

            string value = MoneyDisplayFormatter.Format(netWorth);
            string revenueValue = trackingAvailable ? Dollars(revenue) : "Unavailable";
            string expensesValue = trackingAvailable ? "−" + Dollars(expenses) : "Unavailable";
            string profitValue = trackingAvailable ? (profit < 0m ? "−" : "+") + Dollars(Math.Abs(profit)) : "Unavailable";
            string displayKey = value + "|" + revenueValue + "|" + expensesValue + "|" + profitValue;
            if (displayKey == _displayedValue)
                return;

            _valueText.text = value;
            if (_revenueText != null) _revenueText.text = revenueValue;
            if (_expensesText != null) _expensesText.text = expensesValue;
            if (_profitText != null)
            {
                _profitText.text = profitValue;
                _profitText.color = !trackingAvailable ? Color.white : profit < 0m
                    ? new Color(1f, 0.45f, 0.4f) : new Color(0.4f, 0.95f, 0.55f);
            }
            _displayedValue = displayKey;
        }

        private static string Dollars(decimal amount) => MoneyDisplayFormatter.Format(amount);

        private static Text CreateRow(Transform parent, string label, float y)
        {
            var caption = UIFactory.Text(label + "Label", label, parent, 18);
            PlaceText(caption, 14f, y, 110f, 28f);
            var value = UIFactory.Text(label + "Value", "--", parent, 18, TextAnchor.UpperRight);
            PlaceText(value, 124f, y, 202f, 28f);
            return value;
        }

        /// <summary>Releases the complete UI hierarchy; safe to call more than once.</summary>
        public void Dispose()
        {
            if (_root != null)
            {
                _root.SetActive(false);
                Object.Destroy(_root);
            }

            _root = null;
            _valueText = null;
            _revenueText = _expensesText = _profitText = null;
            _displayedValue = null;
        }

        private static void PlaceText(Text text, float x, float y, float width, float height)
        {
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            text.raycastTarget = false;
        }
    }
}
