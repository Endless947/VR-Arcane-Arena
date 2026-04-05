using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRArcaneArena.Managers;

namespace VRArcaneArena.UI
{
    /// <summary>
    /// Displays spell cooldowns as floating world-space progress bars.
    /// </summary>
    public sealed class CooldownStripUI : MonoBehaviour
    {
        public CooldownTracker cooldownTracker;
        public Transform rightHandAnchor;

        private sealed class SpellRow
        {
            public string displayName;
            public string cooldownKey;
            public float maxCooldown;
            public Color color;
            public RectTransform fillRect;
            public Image fillImage;
            public float barWidth;
        }

        private readonly List<SpellRow> _rows = new List<SpellRow>();

        public void Start()
        {
            if (cooldownTracker == null)
            {
                cooldownTracker = CooldownTracker.Instance;
            }

            var panelObject = new GameObject("CooldownStripPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cam = Camera.main;
            panelObject.transform.SetParent(cam.transform, false);
            panelObject.transform.localPosition = new Vector3(0.52f, -0.02f, 1.24f);
            panelObject.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            panelObject.transform.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);

            var canvas = panelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = panelObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(460f, 300f);

            var scaler = panelObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var panelBackgroundObject = new GameObject("CooldownBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelBackgroundObject.transform.SetParent(panelObject.transform, false);
            var panelBackgroundRect = panelBackgroundObject.GetComponent<RectTransform>();
            panelBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
            panelBackgroundRect.anchoredPosition = Vector2.zero;
            panelBackgroundRect.sizeDelta = new Vector2(440f, 286f);

            var panelBackgroundImage = panelBackgroundObject.GetComponent<Image>();
            panelBackgroundImage.color = new Color(0f, 0f, 0f, 0.58f);
            panelBackgroundImage.raycastTarget = false;

            var titleObject = new GameObject("CooldownTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObject.transform.SetParent(panelObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 124f);
            titleRect.sizeDelta = new Vector2(390f, 24f);

            var titleText = titleObject.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "SPELL COOLDOWNS";
            titleText.color = new Color(1f, 0.92f, 0.6f, 1f);
            titleText.raycastTarget = false;

            const float rowHeight = 22f;
            const float rowPadding = 7f;
            const float labelWidth = 145f;
            const float barWidth = 242f;
            const float topY = 88f;

            var spellRows = new[]
            {
                new { Display = "Fireball", Key = "Fireball", Cooldown = 3f, Color = new Color(1f, 0.4f, 0f) },
                new { Display = "Blizzard", Key = "Blizzard", Cooldown = 8f, Color = Color.cyan },
                new { Display = "Lightning Bolt", Key = "Lightning Bolt", Cooldown = 5f, Color = Color.yellow },
                new { Display = "Arcane Shield", Key = "Arcane Shield", Cooldown = 10f, Color = Color.white },
                new { Display = "Meteor Strike", Key = "Meteor Strike", Cooldown = 20f, Color = Color.red },
                new { Display = "Gravity Well", Key = "Gravity Well", Cooldown = 12f, Color = Color.magenta },
                new { Display = "Frost Nova", Key = "Frost Nova", Cooldown = 6f, Color = new Color(0.5f, 0.8f, 1f) },
                new { Display = "Void Blast", Key = "Void Blast", Cooldown = 15f, Color = new Color(0.4f, 0f, 0.6f) }
            };

            for (var i = 0; i < spellRows.Length; i++)
            {
                var rowY = topY - i * (rowHeight + rowPadding);

                var labelObject = new GameObject($"Label_{spellRows[i].Display}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelObject.transform.SetParent(panelObject.transform, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(-212f, rowY);
                labelRect.sizeDelta = new Vector2(labelWidth, rowHeight);

                var labelText = labelObject.GetComponent<Text>();
                labelText.font = font;
                labelText.fontSize = 15;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.text = spellRows[i].Display;
                labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                labelText.raycastTarget = false;

                var barBackgroundObject = new GameObject($"BarBackground_{spellRows[i].Display}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barBackgroundObject.transform.SetParent(panelObject.transform, false);
                var barBackgroundRect = barBackgroundObject.GetComponent<RectTransform>();
                barBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                barBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                barBackgroundRect.pivot = new Vector2(0f, 0.5f);
                barBackgroundRect.anchoredPosition = new Vector2(-58f, rowY);
                barBackgroundRect.sizeDelta = new Vector2(barWidth, rowHeight);

                var backgroundImage = barBackgroundObject.GetComponent<Image>();
                backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                backgroundImage.raycastTarget = false;

                var fillObject = new GameObject($"BarFill_{spellRows[i].Display}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(barBackgroundObject.transform, false);
                var fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(barWidth, 0f);

                var fillImage = fillObject.GetComponent<Image>();
                fillImage.color = spellRows[i].Color;
                fillImage.raycastTarget = false;

                _rows.Add(new SpellRow
                {
                    displayName = spellRows[i].Display,
                    cooldownKey = spellRows[i].Key,
                    maxCooldown = spellRows[i].Cooldown,
                    color = spellRows[i].Color,
                    fillRect = fillRect,
                    fillImage = fillImage,
                    barWidth = barWidth
                });
            }
        }

        public void Update()
        {
            var tracker = CooldownTracker.Instance != null ? CooldownTracker.Instance : cooldownTracker;
            if (tracker == null)
            {
                return;
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var onCooldown = tracker.IsOnCooldown(row.cooldownKey);

                if (onCooldown)
                {
                    var remaining = tracker.GetRemainingTime(row.cooldownKey);
                    var fraction = row.maxCooldown > 0f ? Mathf.Clamp01(remaining / row.maxCooldown) : 0f;
                    row.fillRect.sizeDelta = new Vector2(row.barWidth * fraction, row.fillRect.sizeDelta.y);
                    row.fillImage.color = row.color;
                }
                else
                {
                    row.fillRect.sizeDelta = new Vector2(row.barWidth, row.fillRect.sizeDelta.y);
                    row.fillImage.color = new Color(row.color.r, row.color.g, row.color.b, 0.3f);
                }
            }
        }
    }
}
