using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Manages WebSocket connection for real-time STT → LLM → TTS conversation
    /// </summary>
    public class ConversationWebSocketManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BackendConfig config;
        [SerializeField] private AudioCaptureManager audioCaptureManager;
        [SerializeField] private AudioPlaybackManager audioPlaybackManager;
        
        [Header("Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string currentNpcId;
        
        // Events
        public event Action<GrammarCorrectionData> OnGrammarCorrection;
        public event Action OnTurnComplete;
        public event Action<string> OnTranscription;
        public event Action<string> OnNpcText;  // NPC's spoken text (for subtitles)
        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private WebSocket webSocket;
        private Queue<byte[]> audioChunksToSend = new Queue<byte[]>();
        private bool isSending = false;
        
        void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            webSocket?.DispatchMessageQueue();
            #endif
        }
        
        /// <summary>
        /// Connect to conversation WebSocket for a specific NPC
        /// </summary>
        public async void Connect(string npcId)
        {
            if (isConnected)
            {
                Debug.LogWarning("[ConversationWS] Already connected. Disconnect first.");
                return;
            }
            
            currentNpcId = npcId;
            string wsUrl = config.GetWebSocketUrl(npcId);
            
            Debug.Log($"[ConversationWS] Connecting to {wsUrl}");
            
            webSocket = new WebSocket(wsUrl);
            
            webSocket.OnOpen += () =>
            {
                Debug.Log("[ConversationWS] Connected!");
                isConnected = true;
                OnConnected?.Invoke();
            };
            
            webSocket.OnError += (e) =>
            {
                Debug.LogError($"[ConversationWS] Error: {e}");
            };
            
            webSocket.OnClose += (e) =>
            {
                Debug.Log($"[ConversationWS] Closed: {e}");
                isConnected = false;
                OnDisconnected?.Invoke();
            };
            
            webSocket.OnMessage += (bytes) =>
            {
                Debug.Log($"[ConversationWS] OnMessage: Received {bytes.Length} bytes");
                HandleMessage(bytes);
            };
            
            await webSocket.Connect();
        }
        
        /// <summary>
        /// Disconnect from WebSocket
        /// </summary>
        public async void Disconnect()
        {
            if (webSocket != null && isConnected)
            {
                await webSocket.Close();
                webSocket = null;
                isConnected = false;
            }
        }
        
        /// <summary>
        /// Send audio chunk to backend
        /// </summary>
        public async void SendAudioChunk(byte[] audioData)
        {
            if (!isConnected || webSocket == null)
            {
                Debug.LogWarning("[ConversationWS] Not connected. Cannot send audio.");
                return;
            }
            
            // Send binary data
            await webSocket.Send(audioData);
        }
        
        /// <summary>
        /// Signal that the player has finished speaking
        /// </summary>
        public async void SendEndUtterance()
        {
            if (!isConnected || webSocket == null)
            {
                Debug.LogWarning("[ConversationWS] Not connected. Cannot send end utterance.");
                return;
            }
            
            var message = new WebSocketMessage { type = "end_utterance" };
            string json = JsonUtility.ToJson(message);
            
            Debug.Log($"[ConversationWS] Sending: {json}");
            await webSocket.SendText(json);
        }
        
        /// <summary>
        /// Handle incoming WebSocket messages
        /// </summary>
        private void HandleMessage(byte[] data)
        {
            // Try to parse as text (JSON)
            try
            {
                string text = Encoding.UTF8.GetString(data);
                
                // Check if it's valid JSON
                if (text.StartsWith("{"))
                {
                    Debug.Log($"[ConversationWS] Detected JSON message");
                    HandleJsonMessage(text);
                    return;
                }
                else
                {
                    Debug.Log($"[ConversationWS] Not JSON (starts with: '{text.Substring(0, Math.Min(20, text.Length))}'), treating as binary audio");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[ConversationWS] Failed to decode as text ({e.Message}), treating as binary audio");
            }
            
            // Handle as binary audio data
            HandleAudioChunk(data);
        }
        
        /// <summary>
        /// Handle JSON text messages
        /// </summary>
        private void HandleJsonMessage(string json)
        {
            Debug.Log($"[ConversationWS] Received JSON: {json}");
            
            try
            {
                var baseMessage = JsonUtility.FromJson<WebSocketMessage>(json);
                
                switch (baseMessage.type)
                {
                    case "grammar_correction":
                        var grammarEvent = JsonUtility.FromJson<GrammarCorrectionEvent>(json);
                        OnGrammarCorrection?.Invoke(grammarEvent.data);
                        break;
                    
                    case "turn_complete":
                        Debug.Log("[ConversationWS] NPC turn complete");
                        OnTurnComplete?.Invoke();
                        break;
                    
                    case "transcription":
                        OnTranscription?.Invoke(baseMessage.text);
                        break;
                    
                    case "npc_text":
                        OnNpcText?.Invoke(baseMessage.text);
                        break;
                    
                    case "error":
                        Debug.LogError($"[ConversationWS] Server error: {baseMessage.text}");
                        break;
                    
                    default:
                        Debug.LogWarning($"[ConversationWS] Unknown message type: {baseMessage.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConversationWS] Failed to parse JSON: {e.Message}");
            }
        }
        
        /// <summary>
        /// Handle incoming audio chunks from NPC
        /// </summary>
        private void HandleAudioChunk(byte[] audioData)
        {
            Debug.Log($"[ConversationWS] Received audio chunk: {audioData.Length} bytes");
            
            // Pass to audio playback manager
            if (audioPlaybackManager != null)
            {
                audioPlaybackManager.QueueAudioChunk(audioData);
            }
        }
        
        void OnDestroy()
        {
            Disconnect();
        }
        
        void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
