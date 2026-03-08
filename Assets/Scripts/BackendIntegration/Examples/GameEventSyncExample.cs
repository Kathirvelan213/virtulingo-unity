using UnityEngine;
using VirtuLingo.Backend;

namespace VirtuLingo.Examples
{
    /// <summary>
    /// Example showing how to sync various game events to the backend
    /// Attach this to your PlayerController or GameManager
    /// </summary>
    public class GameEventSyncExample : MonoBehaviour
    {
        private GameEventSyncManager gameEventSync;
        private string lastHeldObject = null;
        
        void Start()
        {
            gameEventSync = FindObjectOfType<GameEventSyncManager>();
            
            if (gameEventSync == null)
            {
                Debug.LogWarning("[GameEventSyncExample] GameEventSyncManager not found in scene!");
            }
        }
        
        // ========== Example: Item Pickup System ==========
        
        public void OnItemPickedUp(string itemId)
        {
            Debug.Log($"[GameEventSyncExample] Picked up: {itemId}");
            gameEventSync?.OnPickedObject(itemId);
            lastHeldObject = itemId;
        }
        
        public void OnItemDropped(string itemId)
        {
            Debug.Log($"[GameEventSyncExample] Dropped: {itemId}");
            gameEventSync?.OnDroppedObject(itemId);
            lastHeldObject = null;
        }
        
        // ========== Example: Scene Transition ==========
        
        void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[GameEventSyncExample] Scene loaded: {scene.name}");
            gameEventSync?.OnSceneChanged(scene.name);
        }
        
        // ========== Example: Quest System Integration ==========
        
        public void OnQuestStarted(string questId)
        {
            // You can send custom events too
            var payload = new System.Collections.Generic.Dictionary<string, object>
            {
                { "quest_id", questId },
                { "status", "started" }
            };
            
            gameEventSync?.SendEvent("QuestStarted", payload);
        }
        
        public void OnQuestCompleted(string questId)
        {
            var payload = new System.Collections.Generic.Dictionary<string, object>
            {
                { "quest_id", questId },
                { "status", "completed" }
            };
            
            gameEventSync?.SendEvent("QuestCompleted", payload);
        }
        
        // ========== Example: Get Current State from Backend ==========
        
        public void GetCurrentPlayerState()
        {
            gameEventSync?.GetPlayerState(OnPlayerStateReceived);
        }
        
        void OnPlayerStateReceived(PlayerState state)
        {
            if (state == null)
            {
                Debug.LogWarning("[GameEventSyncExample] Failed to get player state");
                return;
            }
            
            Debug.Log($"[GameEventSyncExample] Player State:");
            Debug.Log($"  - Language: {state.language}");
            Debug.Log($"  - Proficiency: {state.proficiency_level}");
            Debug.Log($"  - Scene: {state.scene_id}");
            Debug.Log($"  - Holding: {state.object_in_hand ?? "nothing"}");
            Debug.Log($"  - Active Quest: {state.active_quest ?? "none"}");
            Debug.Log($"  - Nearby NPCs: {state.nearby_npcs?.Count ?? 0}");
        }
    }
}
