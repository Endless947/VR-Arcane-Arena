using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using VRArcaneArena.DataStructures;

namespace VRArcaneArena.Managers
{
    /// <summary>
    /// Converts controller button presses to gesture tokens and resolves spells through a trie.
    /// </summary>
    public sealed class GestureDetector : MonoBehaviour
    {
        public SpellTrie spellTrie;

        public UnityEvent<string> onSpellCast;
        public UnityEvent onInvalidGesture;
        public UnityEvent<List<string>> onReachableSpellsUpdated;

        private bool _prevA;
        private bool _prevB;
        private bool _prevX;
        private bool _prevY;

        public void Awake()
        {
            spellTrie = new SpellTrie();
            spellTrie.LoadDefaultSpells();

            if (onSpellCast == null)           onSpellCast = new UnityEvent<string>();
            if (onInvalidGesture == null)      onInvalidGesture = new UnityEvent();
            if (onReachableSpellsUpdated == null) onReachableSpellsUpdated = new UnityEvent<List<string>>();
        }

        public void Update()
        {
            // Keyboard shortcuts — always work in Editor and as headset fallback
            if (Input.GetKeyDown(KeyCode.Space))      { onSpellCast.Invoke("Fireball");       return; }
            if (Input.GetKeyDown(KeyCode.Alpha1))     { onSpellCast.Invoke("Blizzard");        return; }
            if (Input.GetKeyDown(KeyCode.Alpha2))     { onSpellCast.Invoke("Lightning Bolt");  return; }
            if (Input.GetKeyDown(KeyCode.Alpha3))     { onSpellCast.Invoke("Arcane Shield");   return; }
            if (Input.GetKeyDown(KeyCode.Alpha4))     { onSpellCast.Invoke("Meteor Strike");   return; }
            if (Input.GetKeyDown(KeyCode.Alpha5))     { onSpellCast.Invoke("Gravity Well");    return; }
            if (Input.GetKeyDown(KeyCode.Alpha6))     { onSpellCast.Invoke("Frost Nova");      return; }
            if (Input.GetKeyDown(KeyCode.Alpha7))     { onSpellCast.Invoke("Void Blast");      return; }

            var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            bool currentA = false;
            bool currentB = false;
            bool currentX = false;
            bool currentY = false;

            rightController.TryGetFeatureValue(CommonUsages.primaryButton, out currentA);
            rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out currentB);
            leftController.TryGetFeatureValue(CommonUsages.primaryButton, out currentX);
            leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out currentY);

            if (currentA && !_prevA) ProcessGestureToken('F');
            if (currentB && !_prevB) ProcessGestureToken('P');
            if (currentX && !_prevX) ProcessGestureToken('O');
            if (currentY && !_prevY) ProcessGestureToken('S');

            _prevA = currentA;
            _prevB = currentB;
            _prevX = currentX;
            _prevY = currentY;
        }

        public void ProcessGestureToken(char token)
        {
            var spell = spellTrie.Traverse(token);

            if (!spellTrie.IsValidPrefix())
            {
                onInvalidGesture.Invoke();
                ResetGesture();
                return;
            }

            if (!string.IsNullOrEmpty(spell))
            {
                onSpellCast.Invoke(spell);
                ResetGesture();
                return;
            }

            onReachableSpellsUpdated.Invoke(spellTrie.GetReachableSpells());
        }

        public void ResetGesture()
        {
            spellTrie.Reset();
        }

        public List<string> GetReachableSpells()
        {
            return spellTrie.GetReachableSpells();
        }
    }
}
