using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Syncs game events to backend for world state management
    /// </summary>
    public class GameEventSyncManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig config;
        
        [Header("Sync Settings")]
        [SerializeField] private bool enablePositionSync = false; // Use zone-based tracking instead
        [SerializeField] private float positionSyncInterval = 2f; // seconds
        [SerializeField] private float proximityCheckRadius = 5f;
        
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        
        private float lastPositionSyncTime = 0f;
        private Vector3 lastSyncedPosition;
        private HashSet<string> nearbyNpcs = new HashSet<string>();
        
        void Update()
        {
            // Auto-sync player position periodically (if enabled)
            // NOTE: Prefer using LocationZoneManager for zone-based location tracking
            if (enablePositionSync && Time.time - lastPositionSyncTime >= positionSyncInterval)
            {
                SyncPlayerPosition();
            }
        }
        
        /// <summary>
        /// Send a game event to the backend
        /// </summary>
        public void SendEvent(string eventType, Dictionary<string, object> payload = null)
        {
            var gameEvent = new GameEvent
            {
                player_id = config.playerId,
                event_type = eventType,
                payload = payload ?? new Dictionary<string, object>()
            };
            
            StartCoroutine(PostEventCoroutine(gameEvent));
        }
        
        /// <summary>
        /// Sync player position to backend
        /// </summary>
        public void SyncPlayerPosition()
        {
            if (playerTransform == null)
                return;
            
            Vector3 pos = playerTransform.position;
            
            // Only send if position changed significantly
            if (Vector3.Distance(pos, lastSyncedPosition) < 0.1f)
                return;
            
            var payload = new Dictionary<string, object>
            {
                { "x", pos.x },
                { "y", pos.y },
                { "z", pos.z }
            };
            
            SendEvent("PlayerMoved", payload);
            lastSyncedPosition = pos;
            lastPositionSyncTime = Time.time;
        }
        
        /// <summary>
        /// Notify backend when player picks up an object
        /// </summary>
        public void OnPickedObject(string objectId)
        {
            var payload = new Dictionary<string, object>
            {
                { "object_id", objectId }
            };
            SendEvent("PlayerPickedObject", payload);
        }
        
        /// <summary>
        /// Notify backend when player drops an object
        /// </summary>
        public void OnDroppedObject(string objectId)
        {
            var payload = new Dictionary<string, object>
            {
                { "object_id", objectId }
            };
            SendEvent("PlayerDroppedObject", payload);
        }
        
        /// <summary>
        /// Notify backend when player enters NPC proximity
        /// </summary>
        public void OnEnteredNPCProximity(string npcId)
        {
            if (nearbyNpcs.Contains(npcId))
                return;
            
            nearbyNpcs.Add(npcId);
            
            var payload = new Dictionary<string, object>
            {
                { "npc_id", npcId }
            };
            SendEvent("PlayerEnteredProximity", payload);
        }
        
        /// <summary>
        /// Notify backend when player leaves NPC proximity
        /// </summary>
        public void OnLeftNPCProximity(string npcId)
        {
            if (!nearbyNpcs.Contains(npcId))
                return;
            
            nearbyNpcs.Remove(npcId);
            
            var payload = new Dictionary<string, object>
            {
                { "npc_id", npcId }
            };
            SendEvent("PlayerLeftProximity", payload);
        }
        
        /// <summary>
        /// Notify backend when scene changes
        /// </summary>
        public void OnSceneChanged(string sceneId)
        {
            var payload = new Dictionary<string, object>
            {
                { "scene_id", sceneId }
            };
            SendEvent("SceneChanged", payload);
        }
        
        /// <summary>
        /// Notify backend when dialogue starts
        /// </summary>
        public void OnDialogueStarted(string npcId)
        {
            var payload = new Dictionary<string, object>
            {
                { "npc_id", npcId }
            };
            SendEvent("DialogueStarted", payload);
        }
        
        /// <summary>
        /// Notify backend when dialogue ends
        /// </summary>
        public void OnDialogueEnded(string npcId)
        {
            var payload = new Dictionary<string, object>
            {
                { "npc_id", npcId }
            };
            SendEvent("DialogueEnded", payload);
        }
        
        /// <summary>
        /// Get current player state from backend
        /// </summary>
        public void GetPlayerState(Action<PlayerState> callback)
        {
            StartCoroutine(GetPlayerStateCoroutine(callback));
        }
        
        // ========== HTTP Coroutines ==========

        private IEnumerator PostEventCoroutine(GameEvent gameEvent)
        {
            string url = config.GetEventsUrl();
            // JsonUtility.ToJson silently drops Dictionary fields.
            // Newtonsoft.Json handles Dictionary<string,object> correctly.
            string json = JsonConvert.SerializeObject(gameEvent);

            Debug.Log($"[GameEventSync] Sending event: {gameEvent.event_type} | json: {json}");
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[GameEventSync] Event sent successfully: {gameEvent.event_type}");
                }
                else
                {
                    Debug.LogError($"[GameEventSync] Error sending event: {request.error}\nResponse body: {request.downloadHandler.text}");
                }
            }
        }
        
        private IEnumerator GetPlayerStateCoroutine(Action<PlayerState> callback)
        {
            string url = config.GetStateUrl();
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    PlayerState state = JsonUtility.FromJson<PlayerState>(json);
                    callback?.Invoke(state);
                }
                else
                {
                    Debug.LogError($"[GameEventSync] Error getting state: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}
