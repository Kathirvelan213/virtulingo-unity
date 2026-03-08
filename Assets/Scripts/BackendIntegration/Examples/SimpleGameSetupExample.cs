using UnityEngine;
using VirtuLingo.Backend;

namespace VirtuLingo.Examples
{
    /// <summary>
    /// Complete example setup for a simple game with:
    /// - Zone-based location tracking
    /// - Single NPC conversation
    /// - Push-to-talk (F key)
    /// - Item pickup tracking
    /// 
    /// Setup Instructions:
    /// 1. Attach this to your Player GameObject
    /// 2. Assign all zone colliders in the inspector
    /// 3. Make sure ConversationManager exists in scene
    /// 4. Make sure BackendConfig is configured
    /// 5. Tag your player GameObject as "Player"
    /// 6. Press Play and hold F to talk!
    /// </summary>
    public class SimpleGameSetupExample : MonoBehaviour
    {
        [Header("Zone Setup")]
        [Tooltip("Define all zones in your game world")]
        [SerializeField] private LocationZone[] gameZones = new LocationZone[]
        {
            new LocationZone 
            { 
                zoneId = "bakery", 
                displayName = "The Bakery",
                description = "A warm French bakery filled with the aroma of fresh bread"
            },
            new LocationZone 
            { 
                zoneId = "marketplace", 
                displayName = "Marketplace",
                description = "A bustling outdoor market with vendors selling fresh produce"
            },
            new LocationZone 
            { 
                zoneId = "cafe", 
                displayName = "Street Café",
                description = "A cozy outdoor café with small round tables"
            }
        };
        
        [Header("NPC Configuration")]
        [SerializeField] private string npcId = "baker_01";
        
        [Header("Components (Auto-Found)")]
        private LocationZoneManager locationManager;
        private SimplePushToTalkController talkController;
        private GameEventSyncManager eventSync;
        
        void Start()
        {
            // Setup components
            SetupLocationTracking();
            SetupConversation();
            SetupEventListeners();
            
            Debug.Log("[SimpleGameSetup] ✓ Setup complete! Hold F to talk.");
        }
        
        /// <summary>
        /// Setup zone-based location tracking
        /// </summary>
        private void SetupLocationTracking()
        {
            // Add LocationZoneManager if not present
            locationManager = gameObject.GetComponent<LocationZoneManager>();
            if (locationManager == null)
            {
                locationManager = gameObject.AddComponent<LocationZoneManager>();
            }
            
            // Find or create GameEventSyncManager
            eventSync = FindObjectOfType<GameEventSyncManager>();
            if (eventSync == null)
            {
                Debug.LogWarning("[SimpleGameSetup] GameEventSyncManager not found in scene!");
            }
            
            Debug.Log($"[SimpleGameSetup] ✓ Location tracking ready ({gameZones.Length} zones)");
        }
        
        /// <summary>
        /// Setup conversation system
        /// </summary>
        private void SetupConversation()
        {
            // Find or add SimplePushToTalkController
            talkController = FindObjectOfType<SimplePushToTalkController>();
            if (talkController == null)
            {
                var go = new GameObject("PushToTalkController");
                talkController = go.AddComponent<SimplePushToTalkController>();
                Debug.Log("[SimpleGameSetup] Created PushToTalkController");
            }
            
            Debug.Log("[SimpleGameSetup] ✓ Push-to-talk ready (F key)");
        }
        
        /// <summary>
        /// Setup event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            if (locationManager != null)
            {
                locationManager.OnZoneChanged += OnZoneChanged;
            }
        }
        
        /// <summary>
        /// Called when player enters a new zone
        /// </summary>
        private void OnZoneChanged(string zoneId, string zoneName)
        {
            Debug.Log($"[SimpleGameSetup] 📍 Location changed to: {zoneName}");
            
            // The LocationZoneManager already notifies the backend via GameEventSync
            // You can add additional logic here (e.g., play ambient sounds, change lighting)
        }
        
        // ========== Example: Item Pickup ==========
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Item"))
            {
                string itemId = other.gameObject.name; // or get from a component
                PickupItem(itemId);
            }
        }
        
        void PickupItem(string itemId)
        {
            Debug.Log($"[SimpleGameSetup] Picked up: {itemId}");
            eventSync?.OnPickedObject(itemId);
            
            // Add to inventory, play sound, etc.
        }
        
        void DropItem(string itemId)
        {
            Debug.Log($"[SimpleGameSetup] Dropped: {itemId}");
            eventSync?.OnDroppedObject(itemId);
        }
        
        // ========== Debug Info ==========
        
        void OnGUI()
        {
            if (!Application.isPlaying)
                return;
            
            // Show current state in top-left corner
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"<b>Current Zone:</b> {locationManager?.GetCurrentZoneDisplayName() ?? "Unknown"}");
            GUILayout.Label($"<b>NPC ID:</b> {npcId}");
            GUILayout.Label($"");
            GUILayout.Label($"<b>Controls:</b>");
            GUILayout.Label($"  F - Push to Talk");
            GUILayout.Label($"  ESC - End Conversation");
            GUILayout.EndArea();
        }
    }
}
