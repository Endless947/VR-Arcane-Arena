using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRArcaneArena.Managers;

namespace VRArcaneArena.UI
{
    /// <summary>
    /// Displays the spell trie as a floating world-space UI graph on the left wrist.
    /// </summary>
    public sealed class TrieVisualizer : MonoBehaviour
    {
        public GestureDetector gestureDetector;
        public Transform leftHandAnchor;
        public float nodeRadius = 0.02f;

        private Canvas _canvas;
        private Dictionary<string, Image> _nodeImages;
        private Dictionary<string, RectTransform> _nodeRects;
        private Dictionary<string, Image> _edgeImagesByChildSequence;
        private Dictionary<string, RectTransform> _edgeRectsByChildSequence;

        private Text _statusSequenceText;
        private Text _statusNextGesturesText;
        private Text _statusSpellsText;

        private readonly Dictionary<string, string> _spellSequences = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Fireball", "FP" },
            { "Blizzard", "OOS" },
            { "Lightning Bolt", "PPF" },
            { "Arcane Shield", "SO" },
            { "Meteor Strike", "FFF" },
            { "Gravity Well", "OPS" },
            { "Frost Nova", "PO" },
            { "Void Blast", "SFF" }
        };

        private readonly Dictionary<string, string> _spellNameBySequence = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "FP", "Fireball" },
            { "OOS", "Blizzard" },
            { "PPF", "Lightning Bolt" },
            { "SO", "Arcane Shield" },
            { "FFF", "Meteor Strike" },
            { "OPS", "Gravity Well" },
            { "PO", "Frost Nova" },
            { "SFF", "Void Blast" }
        };

        private readonly Dictionary<string, Vector2> _nodePositions = new Dictionary<string, Vector2>(StringComparer.Ordinal)
        {
            { string.Empty, new Vector2(0f, 138f) },
            { "F", new Vector2(-220f, 78f) },
            { "O", new Vector2(-74f, 78f) },
            { "P", new Vector2(74f, 78f) },
            { "S", new Vector2(220f, 78f) },
            { "FF", new Vector2(-256f, 6f) },
            { "FP", new Vector2(-184f, 6f) },
            { "OO", new Vector2(-98f, 6f) },
            { "OP", new Vector2(-24f, 6f) },
            { "PP", new Vector2(48f, 6f) },
            { "PO", new Vector2(122f, 6f) },
            { "SO", new Vector2(194f, 6f) },
            { "SF", new Vector2(256f, 6f) },
            { "FFF", new Vector2(-256f, -72f) },
            { "OOS", new Vector2(-98f, -72f) },
            { "OPS", new Vector2(-24f, -72f) },
            { "PPF", new Vector2(48f, -72f) },
            { "SFF", new Vector2(256f, -72f) }
        };

        private static readonly Color GreyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f, 1f);
        private static readonly Color RedColor = new Color(1f, 0.2f, 0.2f, 1f);
        private static readonly Color ReachableNodeColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color UnreachableNodeColor = new Color(0.28f, 0.28f, 0.28f, 0.65f);
        private static readonly Color ReachableEdgeColor = new Color(0.72f, 0.72f, 0.72f, 0.8f);
        private static readonly Color UnreachableEdgeColor = new Color(0.35f, 0.35f, 0.35f, 0.3f);

        private static readonly string[] AllSequences =
        {
            "FP",
            "OOS",
            "PPF",
            "SO",
            "FFF",
            "OPS",
            "PO",
            "SFF"
        };

        private string _currentActiveSequence = string.Empty;
        private HashSet<string> _reachableSequences;
        private Coroutine _flashRoutine;

        /// <summary>
        /// Creates the canvas and trie graph.
        /// </summary>
        public void Start()
        {
            Debug.Log("TrieVisualizer Start() called");

            if (gestureDetector == null)
            {
                gestureDetector = GetComponent<GestureDetector>();
            }

            BuildCanvas();
            BuildGraph();
            SubscribeToEvents();

            if (gestureDetector != null)
            {
                HandleReachableSpellsUpdated(gestureDetector.GetReachableSpells());
            }
            else
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
                _currentActiveSequence = string.Empty;
                RefreshVisualState();
            }
        }

        /// <summary>
        /// Unsubscribes from gesture events when this component is disabled.
        /// </summary>
        public void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Unsubscribes from gesture events and stops any active flash animation.
        /// </summary>
        public void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
        }

        private void BuildCanvas()
        {
            var panelObject = new GameObject("TriePanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cam = Camera.main;
            panelObject.transform.SetParent(cam.transform, false);

            panelObject.transform.localPosition = new Vector3(-0.27f, 0.00f, 1.40f);
            panelObject.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            panelObject.transform.localScale = new Vector3(0.00148f, 0.00148f, 0.00148f);

            _canvas = panelObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = _canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(620f, 430f);

            var scaler = panelObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            _nodeImages = new Dictionary<string, Image>(StringComparer.Ordinal);
            _nodeRects = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
            _edgeImagesByChildSequence = new Dictionary<string, Image>(StringComparer.Ordinal);
            _edgeRectsByChildSequence = new Dictionary<string, RectTransform>(StringComparer.Ordinal);

            BuildStatusStrip();

            Debug.Log("TriePanel built, parent is: " + (_canvas != null ? _canvas.gameObject.name : "NULL"));
        }

        private void BuildStatusStrip()
        {
            var statusPanel = new GameObject("TrieStatusPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            statusPanel.transform.SetParent(_canvas.transform, false);

            var rect = statusPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 178f);
            rect.sizeDelta = new Vector2(596f, 86f);

            var panelImage = statusPanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);
            panelImage.raycastTarget = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusSequenceText = CreateStatusText(statusPanel.transform, "StatusSequence", "Sequence: -", new Vector2(0f, 22f), 16, Color.white, font);
            _statusNextGesturesText = CreateStatusText(statusPanel.transform, "StatusNextGestures", "Next: -", new Vector2(0f, -2f), 15, new Color(0.85f, 0.95f, 1f, 1f), font);
            _statusSpellsText = CreateStatusText(statusPanel.transform, "StatusSpells", "Spells: -", new Vector2(0f, -26f), 15, new Color(1f, 0.92f, 0.55f, 1f), font);
        }

        private static Text CreateStatusText(Transform parent, string name, string value, Vector2 position, int fontSize, Color color, Font font)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(560f, 20f);

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = color;
            text.text = value;
            return text;
        }

        private void BuildGraph()
        {
            var rootSprite = (Sprite)null;
            var lineSprite = (Sprite)null;

            foreach (var pair in _nodePositions)
            {
                var node = CreateNode(pair.Key, pair.Value, rootSprite);
                _nodeImages[pair.Key] = node;
                _nodeRects[pair.Key] = node.rectTransform;
            }

            foreach (var pair in _nodePositions)
            {
                if (pair.Key.Length == 0)
                {
                    continue;
                }

                var parentSequence = pair.Key.Substring(0, pair.Key.Length - 1);
                if (!_nodePositions.ContainsKey(parentSequence))
                {
                    continue;
                }

                CreateEdge(parentSequence, pair.Key, lineSprite);
            }

            RefreshVisualState();
        }

        private void SubscribeToEvents()
        {
            if (gestureDetector == null)
            {
                return;
            }

            gestureDetector.onReachableSpellsUpdated.AddListener(HandleReachableSpellsUpdated);
            gestureDetector.onSpellCast.AddListener(HandleSpellCast);
            gestureDetector.onInvalidGesture.AddListener(HandleInvalidGesture);
        }

        private void UnsubscribeFromEvents()
        {
            if (gestureDetector == null)
            {
                return;
            }

            gestureDetector.onReachableSpellsUpdated.RemoveListener(HandleReachableSpellsUpdated);
            gestureDetector.onSpellCast.RemoveListener(HandleSpellCast);
            gestureDetector.onInvalidGesture.RemoveListener(HandleInvalidGesture);
        }

        private Image CreateNode(string sequence, Vector2 position, Sprite sprite)
        {
            var nodeObject = new GameObject(sequence.Length == 0 ? "RootNode" : $"Node_{sequence}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nodeObject.transform.SetParent(_canvas.transform, false);

            var rectTransform = nodeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(68f, 40f);

            var image = nodeObject.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = GreyColor;
            image.raycastTarget = false;
            image.preserveAspect = true;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            labelObj.transform.SetParent(nodeObject.transform, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObj.GetComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.color = Color.black;

            string nodeLabel = sequence.Length == 0 ? "ROOT" : sequence[sequence.Length - 1].ToString();
            nodeLabel = nodeLabel.Replace("F", "Fist").Replace("P", "Point").Replace("O", "Open").Replace("S", "Spread");
            label.text = nodeLabel;

            if (_spellNameBySequence.TryGetValue(sequence, out var spellName))
            {
                var spellLabelObj = new GameObject("SpellLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                spellLabelObj.transform.SetParent(nodeObject.transform, false);
                var spellLabelRect = spellLabelObj.GetComponent<RectTransform>();
                spellLabelRect.anchorMin = new Vector2(0.5f, 0f);
                spellLabelRect.anchorMax = new Vector2(0.5f, 0f);
                spellLabelRect.pivot = new Vector2(0.5f, 1f);
                spellLabelRect.anchoredPosition = new Vector2(0f, -22f);
                spellLabelRect.sizeDelta = new Vector2(108f, 18f);

                var spellLabel = spellLabelObj.GetComponent<UnityEngine.UI.Text>();
                spellLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                spellLabel.fontSize = 12;
                spellLabel.alignment = TextAnchor.MiddleCenter;
                spellLabel.raycastTarget = false;
                spellLabel.color = new Color(1f, 0.84f, 0f);
                spellLabel.text = spellName;
            }

            return image;
        }

        private void CreateEdge(string parentSequence, string childSequence, Sprite sprite)
        {
            var parentPosition = _nodePositions[parentSequence];
            var childPosition = _nodePositions[childSequence];
            var edgeObject = new GameObject($"Edge_{parentSequence}_{childSequence}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            edgeObject.transform.SetParent(_canvas.transform, false);
            edgeObject.transform.SetSiblingIndex(0);

            var rectTransform = edgeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = (parentPosition + childPosition) * 0.5f;

            var delta = childPosition - parentPosition;
            var length = delta.magnitude;
            rectTransform.sizeDelta = new Vector2(length, 3f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = edgeObject.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = ReachableEdgeColor;
            image.raycastTarget = false;

            _edgeImagesByChildSequence[childSequence] = image;
            _edgeRectsByChildSequence[childSequence] = rectTransform;
        }

        private void HandleReachableSpellsUpdated(List<string> reachableSpells)
        {
            if (reachableSpells == null)
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
                _currentActiveSequence = string.Empty;
                RefreshVisualState();
                return;
            }

            _reachableSequences = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spell in reachableSpells)
            {
                if (_spellSequences.TryGetValue(spell, out var sequence))
                {
                    _reachableSequences.Add(sequence);
                }
            }

            _currentActiveSequence = GetCommonPrefix(_reachableSequences);
            RefreshVisualState();
        }

        private void HandleSpellCast(string spellName)
        {
            if (_spellSequences.TryGetValue(spellName, out var sequence))
            {
                _currentActiveSequence = sequence;
            }

            if (_reachableSequences == null || _reachableSequences.Count == 0)
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashTerminalNode());
        }

        private void HandleInvalidGesture()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashInvalidState());
        }

        private IEnumerator FlashTerminalNode()
        {
            RefreshVisualState();
            yield return new WaitForSeconds(0.5f);
            _currentActiveSequence = string.Empty;
            _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            RefreshVisualState();
            _flashRoutine = null;
        }

        private IEnumerator FlashInvalidState()
        {
            var activeSequenceBeforeInvalid = _currentActiveSequence;
            SetNodesForInvalidFlash(activeSequenceBeforeInvalid);
            yield return new WaitForSeconds(0.3f);
            _currentActiveSequence = string.Empty;
            _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            RefreshVisualState();
            _flashRoutine = null;
        }

        private void SetNodesForInvalidFlash(string activeSequence)
        {
            if (_nodeImages == null)
            {
                return;
            }

            foreach (var pair in _nodeImages)
            {
                if (pair.Key.Length == 0)
                {
                    pair.Value.color = RedColor;
                    continue;
                }

                pair.Value.color = activeSequence.StartsWith(pair.Key, StringComparison.Ordinal) ? RedColor : GreyColor;
            }

            if (_edgeImagesByChildSequence == null)
            {
                return;
            }

            foreach (var pair in _edgeImagesByChildSequence)
            {
                pair.Value.color = activeSequence.StartsWith(pair.Key, StringComparison.Ordinal) ? RedColor : UnreachableEdgeColor;
            }
        }

        private void RefreshVisualState()
        {
            if (_nodeImages == null || _nodeImages.Count == 0)
            {
                return;
            }

            var activeSequence = _currentActiveSequence ?? string.Empty;
            var reachableSequences = _reachableSequences ?? new HashSet<string>(AllSequences, StringComparer.Ordinal);

            foreach (var pair in _nodeImages)
            {
                var sequence = pair.Key;
                var image = pair.Value;

                if (sequence.Length == 0)
                {
                    image.color = GoldColor;
                    continue;
                }

                if (activeSequence.StartsWith(sequence, StringComparison.Ordinal))
                {
                    image.color = GoldColor;
                    continue;
                }

                var isReachable = false;
                foreach (var reachableSequence in reachableSequences)
                {
                    if (reachableSequence.StartsWith(sequence, StringComparison.Ordinal))
                    {
                        isReachable = true;
                        break;
                    }
                }

                image.color = isReachable ? ReachableNodeColor : UnreachableNodeColor;
            }

            if (_edgeImagesByChildSequence != null)
            {
                foreach (var pair in _edgeImagesByChildSequence)
                {
                    var childSequence = pair.Key;
                    var edgeImage = pair.Value;
                    if (activeSequence.StartsWith(childSequence, StringComparison.Ordinal))
                    {
                        edgeImage.color = GoldColor;
                    }
                    else
                    {
                        var isReachableEdge = false;
                        foreach (var reachableSequence in reachableSequences)
                        {
                            if (reachableSequence.StartsWith(childSequence, StringComparison.Ordinal))
                            {
                                isReachableEdge = true;
                                break;
                            }
                        }

                        edgeImage.color = isReachableEdge ? ReachableEdgeColor : UnreachableEdgeColor;
                    }
                }
            }

            UpdateStatusStrip(activeSequence, reachableSequences);
        }

        private void Update()
        {
            AnimateActivePath();
        }

        private void AnimateActivePath()
        {
            if (_nodeRects == null || _nodeRects.Count == 0)
            {
                return;
            }

            var activeSequence = _currentActiveSequence ?? string.Empty;
            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.06f;

            foreach (var pair in _nodeRects)
            {
                var sequence = pair.Key;
                var rect = pair.Value;
                if (rect == null)
                {
                    continue;
                }

                bool onActivePath = sequence.Length > 0 && activeSequence.StartsWith(sequence, StringComparison.Ordinal);
                rect.localScale = onActivePath ? new Vector3(pulse, pulse, 1f) : Vector3.one;
            }

            if (_edgeRectsByChildSequence == null)
            {
                return;
            }

            foreach (var pair in _edgeRectsByChildSequence)
            {
                var childSequence = pair.Key;
                var rect = pair.Value;
                if (rect == null)
                {
                    continue;
                }

                var edgeSize = rect.sizeDelta;
                edgeSize.y = activeSequence.StartsWith(childSequence, StringComparison.Ordinal) ? 6f : 3f;
                rect.sizeDelta = edgeSize;
            }
        }

        private void UpdateStatusStrip(string activeSequence, ICollection<string> reachableSequences)
        {
            if (_statusSequenceText != null)
            {
                _statusSequenceText.text = "Sequence: " + FormatSequence(activeSequence);
            }

            if (_statusNextGesturesText != null)
            {
                _statusNextGesturesText.text = "Next: " + GetNextGestureLabels(activeSequence, reachableSequences);
            }

            if (_statusSpellsText != null)
            {
                _statusSpellsText.text = "Spells: " + GetPossibleSpellLabels(activeSequence, reachableSequences);
            }
        }

        private string GetNextGestureLabels(string activeSequence, ICollection<string> reachableSequences)
        {
            var nextLabels = new List<string>();
            var seen = new HashSet<char>();

            foreach (var sequence in reachableSequences)
            {
                if (!sequence.StartsWith(activeSequence, StringComparison.Ordinal))
                {
                    continue;
                }

                if (sequence.Length <= activeSequence.Length)
                {
                    continue;
                }

                var nextToken = sequence[activeSequence.Length];
                if (!seen.Add(nextToken))
                {
                    continue;
                }

                nextLabels.Add(TokenToGestureLabel(nextToken));
            }

            return nextLabels.Count == 0 ? "-" : string.Join(" | ", nextLabels);
        }

        private string GetPossibleSpellLabels(string activeSequence, ICollection<string> reachableSequences)
        {
            var spells = new List<string>();

            foreach (var sequence in reachableSequences)
            {
                if (!sequence.StartsWith(activeSequence, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_spellNameBySequence.TryGetValue(sequence, out var spellName))
                {
                    spells.Add(spellName);
                }
            }

            return spells.Count == 0 ? "-" : string.Join(", ", spells);
        }

        private static string TokenToGestureLabel(char token)
        {
            switch (token)
            {
                case 'F': return "Fist (A)";
                case 'P': return "Point (B)";
                case 'O': return "Open (X)";
                case 'S': return "Spread (Y)";
                default: return token.ToString();
            }
        }

        private static string FormatSequence(string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
            {
                return "-";
            }

            var labels = new List<string>(sequence.Length);
            for (var i = 0; i < sequence.Length; i++)
            {
                labels.Add(TokenToGestureLabel(sequence[i]));
            }

            return string.Join(" > ", labels);
        }

        private string GetCommonPrefix(ICollection<string> sequences)
        {
            if (sequences == null || sequences.Count == 0)
            {
                return string.Empty;
            }

            string prefix = null;
            foreach (var sequence in sequences)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = sequence;
                    continue;
                }

                var limit = Mathf.Min(prefix.Length, sequence.Length);
                var index = 0;
                while (index < limit && prefix[index] == sequence[index])
                {
                    index++;
                }

                prefix = prefix.Substring(0, index);
                if (prefix.Length == 0)
                {
                    break;
                }
            }

            return prefix ?? string.Empty;
        }
    }
}
