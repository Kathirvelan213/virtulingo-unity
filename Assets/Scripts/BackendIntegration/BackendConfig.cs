using UnityEngine;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Configuration for backend API connection
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "VirtuLingo/Backend Config")]
    public class BackendConfig : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("Backend server URL (e.g., localhost:8000 or your production domain)")]
        public string serverUrl = "localhost:8000";
        
        [Tooltip("Use WSS (secure WebSocket) instead of WS")]
        public bool useSecureWebSocket = false;
        
        [Tooltip("Use HTTPS instead of HTTP")]
        public bool useHttps = false;
        
        [Header("Player Settings")]
        public string playerId = "player123";
        
        [Header("Audio Settings")]
        [Tooltip("Sample rate for audio recording (backend expects 16000)")]
        public int sampleRate = 16000;
        
        [Tooltip("Audio chunk size in samples")]
        public int chunkSize = 4096;
        
        [Tooltip("Microphone device (leave empty for default)")]
        public string microphoneDevice = "";
        
        // Computed properties
        public string WebSocketProtocol => useSecureWebSocket ? "wss" : "ws";
        public string HttpProtocol => useHttps ? "https" : "http";
        
        public string GetWebSocketUrl(string npcId) =>
            $"{WebSocketProtocol}://{serverUrl}/api/conversations/ws/{playerId}/{npcId}";
        
        public string GetEventsUrl() =>
            $"{HttpProtocol}://{serverUrl}/api/events/";
        
        public string GetStateUrl() =>
            $"{HttpProtocol}://{serverUrl}/api/events/{playerId}/state";
        
        public string GetReviewCheckUrl() =>
            $"{HttpProtocol}://{serverUrl}/api/review/check";
    }
}
