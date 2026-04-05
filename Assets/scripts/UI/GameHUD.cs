using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using VRArcaneArena.Game;

namespace VRArcaneArena.UI
{
    public sealed class GameHUD : MonoBehaviour
    {
        private const float HealthBarWidth = 300f;
        private const float HealthBarHeight = 24f;
        private const float HoldDurationSeconds = 5f;
        private const KeyCode LeftTestKey = KeyCode.L;
        private const KeyCode RightTestKey = KeyCode.K;

        private Text _waveLabel;
        private Text _scoreLabel;
        private RectTransform _healthFillRect;
        private GameObject _gameOverPanel;
        private GameObject _youWinPanel;
        private Text _gameOverScoreText;
        private Text _youWinScoreText;
        private GameObject _startButtonPanel;
        private Text _startButtonHintText;
        private float _dualTriggerHoldTimer;

        private readonly List<InputDevice> _leftDevices = new List<InputDevice>();
        private readonly List<InputDevice> _rightDevices = new List<InputDevice>();

        private void Start()
        {
            var canvasObject = new GameObject("GameHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cam = Camera.main;
            canvasObject.transform.SetParent(cam.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1200f, 80f);

            canvasObject.transform.localPosition = new Vector3(0f, 0.3f, 1.2f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateImage(
                "WaveLabelBackground",
                canvasObject.transform,
                new Color(0f, 0f, 0f, 0.6f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-550f, 25f),
                new Vector2(320f, 40f));

            _waveLabel = CreateText(
                "WaveLabel",
                canvasObject.transform,
                "Wave 1 / 5",
                TextAnchor.UpperLeft,
                28,
                new Color(1f, 0.84f, 0f, 1f),
                font,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-550f, 25f),
                new Vector2(320f, 40f));

            CreateImage(
                "ScoreLabelBackground",
                canvasObject.transform,
                new Color(0f, 0f, 0f, 0.6f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(550f, 25f),
                new Vector2(320f, 40f));

            _scoreLabel = CreateText(
                "ScoreLabel",
                canvasObject.transform,
                "Score: 0",
                TextAnchor.UpperRight,
                28,
                new Color(1f, 0.84f, 0f, 1f),
                font,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(550f, 25f),
                new Vector2(320f, 40f));

            var healthBackground = CreateImage(
                "HealthBackground",
                canvasObject.transform,
                new Color(0.3f, 0f, 0f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, 25f),
                new Vector2(HealthBarWidth, HealthBarHeight));

            var healthFill = CreateImage(
                "HealthFill",
                healthBackground.transform,
                new Color(1f, 0f, 0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(HealthBarWidth, HealthBarHeight));
            _healthFillRect = healthFill.rectTransform;

            _gameOverPanel = CreateCenterPanel(canvasObject.transform, "GameOverPanel");
            _gameOverPanel.SetActive(false);
            CreateText(
                "GameOverTitle",
                _gameOverPanel.transform,
                "GAME OVER",
                TextAnchor.MiddleCenter,
                64,
                Color.red,
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(700f, 90f));
            _gameOverScoreText = CreateText(
                "GameOverScore",
                _gameOverPanel.transform,
                "Final Score: 0",
                TextAnchor.MiddleCenter,
                36,
                Color.red,
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f),
                new Vector2(700f, 70f));

            _youWinPanel = CreateCenterPanel(canvasObject.transform, "YouWinPanel");
            _youWinPanel.SetActive(false);
            var gold = new Color(1f, 0.84f, 0f, 1f);
            CreateText(
                "YouWinTitle",
                _youWinPanel.transform,
                "YOU WIN!",
                TextAnchor.MiddleCenter,
                64,
                gold,
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(700f, 90f));
            _youWinScoreText = CreateText(
                "YouWinScore",
                _youWinPanel.transform,
                "Final Score: 0",
                TextAnchor.MiddleCenter,
                36,
                gold,
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f),
                new Vector2(700f, 70f));

            _startButtonPanel = CreateActionButtonPanel(canvasObject.transform, "StartButtonPanel", "START GAME");
            _startButtonHintText = CreateText(
                "StartButtonHint",
                _startButtonPanel.transform,
                "Hold both front triggers (or L + K) for 5.0s",
                TextAnchor.MiddleCenter,
                26,
                Color.white,
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f),
                new Vector2(860f, 60f));
        }

        private void Update()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _waveLabel.text = "Wave " + gameManager.CurrentWave + " / " + gameManager.totalWaves;
                _scoreLabel.text = "Score: " + gameManager.CurrentPoints;

                bool waitingForStart = !gameManager.HasGameStarted && !gameManager.IsGameOver && !gameManager.IsGameWon;
                if (_startButtonPanel != null)
                {
                    _startButtonPanel.SetActive(waitingForStart);
                }

                if (waitingForStart)
                {
                    HandleStartHold();
                }
                else
                {
                    _dualTriggerHoldTimer = 0f;
                }

                if (gameManager.IsGameOver)
                {
                    _gameOverPanel.SetActive(true);
                    _gameOverScoreText.text = "Final Score: " + gameManager.CurrentPoints;
                }

                if (gameManager.IsGameWon)
                {
                    _youWinPanel.SetActive(true);
                    _youWinScoreText.text = "Final Score: " + gameManager.CurrentPoints;
                }
            }

            var healthPercent = 1f;
            if (PlayerHealth.Instance != null)
            {
                healthPercent = Mathf.Clamp01(PlayerHealth.Instance.HealthPercent);
            }

            var size = _healthFillRect.sizeDelta;
            size.x = HealthBarWidth * healthPercent;
            _healthFillRect.sizeDelta = size;
        }

        private void HandleStartHold()
        {
            bool areBothFrontTriggersPressed = IsTriggerPressed(XRNode.LeftHand, _leftDevices) &&
                                               IsTriggerPressed(XRNode.RightHand, _rightDevices);
            bool areBothTestKeysPressed = Input.GetKey(LeftTestKey) && Input.GetKey(RightTestKey);
            bool isHoldInputPressed = areBothFrontTriggersPressed || areBothTestKeysPressed;

            if (isHoldInputPressed)
            {
                _dualTriggerHoldTimer += Time.deltaTime;
            }
            else
            {
                _dualTriggerHoldTimer = 0f;
            }

            float secondsLeft = Mathf.Max(0f, HoldDurationSeconds - _dualTriggerHoldTimer);
            if (_startButtonHintText != null)
            {
                _startButtonHintText.text = "Hold both front triggers (or L + K) for " + secondsLeft.ToString("0.0") + "s";
            }

            if (_dualTriggerHoldTimer < HoldDurationSeconds)
            {
                return;
            }

            _dualTriggerHoldTimer = 0f;
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        private static bool IsTriggerPressed(XRNode handNode, List<InputDevice> cachedDevices)
        {
            if (cachedDevices.Count == 0)
            {
                InputDevices.GetDevicesAtXRNode(handNode, cachedDevices);
            }

            for (int i = 0; i < cachedDevices.Count; i++)
            {
                var device = cachedDevices[i];
                if (!device.isValid)
                {
                    continue;
                }

                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonPressed) && triggerButtonPressed)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0.85f)
                {
                    return true;
                }
            }

            cachedDevices.Clear();
            return false;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            TextAnchor alignment,
            int fontSize,
            Color color,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = color;
            text.font = font;

            return text;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = imageObject.GetComponent<Image>();
            image.color = color;

            return image;
        }

        private static GameObject CreateCenterPanel(Transform parent, string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 320f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.65f);

            return panel;
        }

        private static GameObject CreateActionButtonPanel(Transform parent, string name, string title)
        {
            var panel = CreateCenterPanel(parent, name);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, -130f);
            panelRect.sizeDelta = new Vector2(980f, 220f);

            var backgroundImage = panel.GetComponent<Image>();
            backgroundImage.color = new Color(0f, 0.2f, 0.4f, 0.85f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateText(
                name + "Title",
                panel.transform,
                title,
                TextAnchor.MiddleCenter,
                42,
                new Color(0.98f, 0.94f, 0.65f, 1f),
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 28f),
                new Vector2(860f, 80f));

            return panel;
        }
    }
}
