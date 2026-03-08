using UnityEngine;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Tag any scene object with this component to make it grabbable by the player.
    ///
    /// <b>contextName</b> is what gets sent to the backend as "object_in_hand".
    /// It should be a short, natural-language description the LLM can use in conversation.
    ///
    /// Examples:
    ///   contextName = "a red apple (price: 1.50 €)"
    ///   contextName = "a baguette"
    ///   contextName = "a French newspaper"
    ///
    /// The NPC system prompt receives: "Player is holding: a red apple (price: 1.50 €)"
    /// so when the player asks "what's the price of this?" the NPC can answer correctly.
    /// </summary>
    public class GrabbableObject : MonoBehaviour
    {
        [Header("LLM Context")]
        [Tooltip("Human-readable description sent to the LLM. Be specific: 'a baguette (0.90 €)'.")]
        [SerializeField] private string contextName = "an object";

        [Header("Grab Behaviour")]
        [Tooltip("If true, the object becomes kinematic while held and returns to physics when dropped.")]
        [SerializeField] private bool usePhysics = true;

        [Tooltip("Local offset from the hand anchor where this object should sit.")]
        [SerializeField] private Vector3 holdOffset = Vector3.zero;

        [Tooltip("Local rotation applied while held.")]
        [SerializeField] private Vector3 holdRotation = Vector3.zero;

        // ── Runtime state ───────────────────────────────────────────────────────

        private Rigidbody _rb;
        private bool _isHeld;

        /// <summary>The description received by the LLM.</summary>
        public string ContextName => contextName;

        /// <summary>Whether this object is currently held by the player.</summary>
        public bool IsHeld => _isHeld;

        // ── Optional highlight support ──────────────────────────────────────────

        [Header("Highlight (optional)")]
        [Tooltip("Renderer that will be tinted when the player looks at this object. Leave null to skip.")]
        [SerializeField] private Renderer highlightRenderer;

        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0f, 1f);  // gold

        private Color _originalColor;
        private bool _isHighlighted;

        // ───────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        // ── Called by PlayerGrabController ─────────────────────────────────────

        /// <summary>
        /// Attaches this object to <paramref name="anchor"/> and freezes physics.
        /// </summary>
        public void OnGrab(Transform anchor)
        {
            _isHeld = true;

            if (_rb != null && usePhysics)
            {
                _rb.isKinematic = true;
                _rb.linearVelocity    = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            transform.SetParent(anchor, worldPositionStays: false);
            transform.localPosition = holdOffset;
            transform.localEulerAngles = holdRotation;

            SetHighlight(false);
        }

        /// <summary>
        /// Detaches this object and restores physics.
        /// </summary>
        public void OnRelease()
        {
            _isHeld = false;

            transform.SetParent(null);

            if (_rb != null && usePhysics)
                _rb.isKinematic = false;
        }

        // ── Highlight helpers ───────────────────────────────────────────────────

        /// <summary>Tint the object to hint that it is interactable.</summary>
        public void SetHighlight(bool active)
        {
            if (highlightRenderer == null || _isHighlighted == active)
                return;

            _isHighlighted = active;

            if (active)
            {
                _originalColor = highlightRenderer.material.color;
                highlightRenderer.material.color = highlightColor;
            }
            else
            {
                highlightRenderer.material.color = _originalColor;
            }
        }
    }
}
