using UnityEngine;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// First-person grab controller.
    ///
    /// ─ HOW IT WORKS ────────────────────────────────────────────────────────────
    /// Each frame a ray is cast from <see cref="grabOrigin"/> (usually the camera).
    /// If the ray hits a <see cref="GrabbableObject"/> within grab range the object
    /// is highlighted and the grab prompt is shown.
    ///
    /// Press <see cref="grabKey"/> (default: E) to grab / drop.
    ///
    /// On pickup  → GrabbableObject.OnGrab()  and GameEventSyncManager.OnPickedObject()
    /// On release → GrabbableObject.OnRelease() and GameEventSyncManager.OnDroppedObject()
    ///
    /// The <c>contextName</c> field on GrabbableObject is what the backend sends to the
    /// LLM, so "Player is holding: a red apple (price: 1.50 €)" ends up in the NPC prompt.
    ///
    /// ─ SETUP ────────────────────────────────────────────────────────────────────
    /// 1. Add this component to your Player GameObject.
    /// 2. Assign <b>Grab Origin</b>  → your Main Camera (or a hand bone).
    /// 3. Assign <b>Hand Anchor</b>  → an empty child of the camera / hand where
    ///    held objects will be parented.
    /// 4. Assign <b>Event Sync</b>   → the GameEventSyncManager in the scene.
    /// 5. (Optional) Assign <b>Grab Prompt Text</b> → a UI Text/TMP label to show hints.
    ///
    /// ─ MAKING AN OBJECT GRABBABLE ────────────────────────────────────────────────
    /// Add a <see cref="GrabbableObject"/> component to any scene object and set its
    /// <b>Context Name</b> field, e.g. "a baguette (0.90 €)".
    /// Make sure the object has a Collider so the raycast can hit it.
    /// </summary>
    public class PlayerGrabController : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("Source of the grab ray – typically the Main Camera.")]
        [SerializeField] private Transform grabOrigin;

        [Tooltip("The transform held objects are parented to while carried.")]
        [SerializeField] private Transform handAnchor;

        [Tooltip("GameEventSyncManager used to notify the backend.")]
        [SerializeField] private GameEventSyncManager eventSync;

        [Header("Settings")]
        [Tooltip("Maximum distance (metres) at which the player can grab an object.")]
        [SerializeField] private float grabRange = 2.5f;

        [Tooltip("Layer mask – only these layers are checked by the grab raycast.")]
        [SerializeField] private LayerMask grabbableLayers = Physics.DefaultRaycastLayers;

        [Tooltip("Key used to grab or drop an object.")]
        [SerializeField] private KeyCode grabKey = KeyCode.E;

        [Header("UI (optional)")]
        [Tooltip("Text label that shows grab/drop hints. Leave null to disable.")]
        [SerializeField] private TMPro.TMP_Text grabPromptText;

        // ── Runtime ─────────────────────────────────────────────────────────────

        private GrabbableObject _heldObject;        // currently held object (null if empty hands)
        private GrabbableObject _lookedAtObject;    // object the crosshair is aimed at

        // ────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (grabOrigin == null)
            {
                // Fall back to the camera on this object, then the main camera
                Camera cam = GetComponentInChildren<Camera>();
                if (cam == null) cam = Camera.main;
                if (cam != null) grabOrigin = cam.transform;
            }

            if (handAnchor == null)
            {
                // Auto-create a sensible default anchor in front of the camera
                var anchor = new GameObject("HandAnchor").transform;
                anchor.SetParent(grabOrigin != null ? grabOrigin : transform, worldPositionStays: false);
                anchor.localPosition = new Vector3(0.3f, -0.25f, 0.6f);
                handAnchor = anchor;
            }

            if (eventSync == null)
                eventSync = FindFirstObjectByType<GameEventSyncManager>();
        }

        private void Update()
        {
            UpdateLookTarget();

            if (Input.GetKeyDown(grabKey))
            {
                if (_heldObject != null)
                    DropObject();
                else if (_lookedAtObject != null)
                    GrabObject(_lookedAtObject);
            }

            UpdatePrompt();
        }

        // ── Core logic ──────────────────────────────────────────────────────────

        /// <summary>Cast a ray and bookmark the GrabbableObject we're looking at.</summary>
        private void UpdateLookTarget()
        {
            GrabbableObject newTarget = null;

            if (_heldObject == null && grabOrigin != null)
            {
                if (Physics.Raycast(grabOrigin.position, grabOrigin.forward,
                                    out RaycastHit hit, grabRange, grabbableLayers))
                {
                    newTarget = hit.collider.GetComponentInParent<GrabbableObject>();
                }
            }

            // Handle highlight transitions
            if (newTarget != _lookedAtObject)
            {
                _lookedAtObject?.SetHighlight(false);
                newTarget?.SetHighlight(true);
                _lookedAtObject = newTarget;
            }
        }

        /// <summary>Attach the object to the player's hand and notify the backend.</summary>
        private void GrabObject(GrabbableObject obj)
        {
            _heldObject = obj;
            obj.OnGrab(handAnchor);

            if (eventSync != null)
                eventSync.OnPickedObject(obj.ContextName);
            else
                Debug.LogWarning("[PlayerGrabController] No GameEventSyncManager assigned – backend not notified.");

            Debug.Log($"[PlayerGrabController] Grabbed: {obj.ContextName}");
        }

        /// <summary>Release the held object and notify the backend.</summary>
        private void DropObject()
        {
            if (_heldObject == null) return;

            string contextName = _heldObject.ContextName;
            _heldObject.OnRelease();

            if (eventSync != null)
                eventSync.OnDroppedObject(contextName);

            Debug.Log($"[PlayerGrabController] Dropped: {contextName}");
            _heldObject = null;
        }

        // ── UI ──────────────────────────────────────────────────────────────────

        private void UpdatePrompt()
        {
            if (grabPromptText == null) return;

            if (_heldObject != null)
            {
                grabPromptText.text = $"[{grabKey}] Drop  {_heldObject.ContextName}";
                grabPromptText.gameObject.SetActive(true);
            }
            else if (_lookedAtObject != null)
            {
                grabPromptText.text = $"[{grabKey}] Pick up  {_lookedAtObject.ContextName}";
                grabPromptText.gameObject.SetActive(true);
            }
            else
            {
                grabPromptText.gameObject.SetActive(false);
            }
        }

        // ── Editor helpers ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (grabOrigin == null) return;
            Gizmos.color = _heldObject != null ? Color.green : Color.yellow;
            Gizmos.DrawRay(grabOrigin.position, grabOrigin.forward * grabRange);
        }
#endif

        // ── Public API (for animation / UI integration) ─────────────────────────

        /// <summary>The object currently in the player's hand, or null.</summary>
        public GrabbableObject HeldObject => _heldObject;

        /// <summary>The object the player is aiming at (not yet grabbed), or null.</summary>
        public GrabbableObject LookedAtObject => _lookedAtObject;

        /// <summary>Force a drop from external code (e.g. on dialogue end or scene change).</summary>
        public void ForceDropIfHolding()
        {
            if (_heldObject != null)
                DropObject();
        }
    }
}
