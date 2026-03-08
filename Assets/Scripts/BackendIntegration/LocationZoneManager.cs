using UnityEngine;
using System.Collections.Generic;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Defines a location zone that player can enter
    /// The backend uses zone names for contextual conversation
    /// </summary>
    [System.Serializable]
    public class LocationZone
    {
        [Tooltip("Unique zone identifier (e.g., 'bakery', 'marketplace', 'town_square')")]
        public string zoneId = "marketplace";
        
        [Tooltip("Display name for the zone")]
        public string displayName = "Marketplace";
        
        [Tooltip("Zone trigger collider")]
        public Collider zoneTrigger;
        
        [Tooltip("Description of this location (used in NPC context)")]
        [TextArea(2, 4)]
        public string description = "A bustling French marketplace with fresh produce and baked goods";
    }
    
    /// <summary>
    /// Tracks player location by zones instead of raw coordinates
    /// When player enters a new zone, it updates the backend state
    /// This gives NPCs contextual awareness (e.g., "We're at the bakery")
    /// </summary>
    public class LocationZoneManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameEventSyncManager gameEventSync;
        
        [Header("Zones")]
        [SerializeField] private List<LocationZone> zones = new List<LocationZone>();
        
        [Header("Current State")]
        [SerializeField] private string currentZoneId = "";
        [SerializeField] private string currentZoneDisplayName = "";
        
        // Events
        public event System.Action<string, string> OnZoneChanged; // (zoneId, displayName)
        
        private void Start()
        {
            // Auto-setup zone triggers if they have colliders
            SetupZoneTriggers();
            
            // Set initial zone if player starts in one
            DetectInitialZone();
        }
        
        /// <summary>
        /// Setup zone trigger events
        /// </summary>
        private void SetupZoneTriggers()
        {
            foreach (var zone in zones)
            {
                if (zone.zoneTrigger != null)
                {
                    // Make sure it's a trigger
                    zone.zoneTrigger.isTrigger = true;
                    
                    // Add ZoneTriggerHelper component if not present
                    var helper = zone.zoneTrigger.gameObject.GetComponent<ZoneTriggerHelper>();
                    if (helper == null)
                    {
                        helper = zone.zoneTrigger.gameObject.AddComponent<ZoneTriggerHelper>();
                    }
                    helper.Initialize(this, zone.zoneId, zone.displayName);
                }
            }
        }
        
        /// <summary>
        /// Detect which zone player is currently in at start
        /// </summary>
        private void DetectInitialZone()
        {
            var playerCollider = GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogWarning("[LocationZone] No collider on player for zone detection");
                return;
            }
            
            foreach (var zone in zones)
            {
                if (zone.zoneTrigger != null && zone.zoneTrigger.bounds.Contains(transform.position))
                {
                    EnterZone(zone.zoneId, zone.displayName);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Called when player enters a new zone
        /// </summary>
        public void EnterZone(string zoneId, string displayName)
        {
            if (currentZoneId == zoneId)
                return; // Already in this zone
            
            Debug.Log($"[LocationZone] Entered zone: {displayName} ({zoneId})");
            
            string previousZone = currentZoneId;
            currentZoneId = zoneId;
            currentZoneDisplayName = displayName;
            
            // Notify backend of scene/location change
            if (gameEventSync != null)
            {
                gameEventSync.OnSceneChanged(zoneId);
            }
            
            // Fire event
            OnZoneChanged?.Invoke(zoneId, displayName);
            
            // Optional: You could also send a custom event with more detail
            /*
            var payload = new Dictionary<string, object>
            {
                { "zone_id", zoneId },
                { "zone_name", displayName },
                { "previous_zone", previousZone }
            };
            gameEventSync?.SendEvent("ZoneChanged", payload);
            */
        }
        
        /// <summary>
        /// Get current zone ID
        /// </summary>
        public string GetCurrentZoneId() => currentZoneId;
        
        /// <summary>
        /// Get current zone display name
        /// </summary>
        public string GetCurrentZoneDisplayName() => currentZoneDisplayName;
        
        /// <summary>
        /// Get zone by ID
        /// </summary>
        public LocationZone GetZone(string zoneId)
        {
            return zones.Find(z => z.zoneId == zoneId);
        }
        
        /// <summary>
        /// Manually set zone (useful for teleportation, scene loading, etc.)
        /// </summary>
        public void SetZone(string zoneId)
        {
            var zone = GetZone(zoneId);
            if (zone != null)
            {
                EnterZone(zone.zoneId, zone.displayName);
            }
            else
            {
                Debug.LogWarning($"[LocationZone] Zone not found: {zoneId}");
            }
        }
    }
    
    /// <summary>
    /// Helper component attached to zone trigger colliders
    /// Reports zone entry to LocationZoneManager
    /// </summary>
    public class ZoneTriggerHelper : MonoBehaviour
    {
        private LocationZoneManager manager;
        private string zoneId;
        private string displayName;
        
        public void Initialize(LocationZoneManager mgr, string id, string name)
        {
            manager = mgr;
            zoneId = id;
            displayName = name;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (other.CompareTag("Player") && manager != null)
            {
                manager.EnterZone(zoneId, displayName);
            }
        }
    }
}
