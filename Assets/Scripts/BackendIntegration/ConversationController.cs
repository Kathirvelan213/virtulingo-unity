using UnityEngine;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Main controller that ties everything together for NPC conversations
    /// Attach this to your ConversationManager GameObject
    /// </summary>
    public class ConversationController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ConversationWebSocketManager webSocketManager;
        [SerializeField] private AudioCaptureManager audioCaptureManager;
        [SerializeField] private AudioPlaybackManager audioPlaybackManager;
        [SerializeField] private GameEventSyncManager gameEventSync;
        [SerializeField] private GrammarFeedbackUI grammarFeedbackUI;
        
        [Header("Current Conversation")]
        [SerializeField] private string currentNpcId;
        [SerializeField] private bool isInConversation = false;
        
        void Start()
        {
            SetupEventListeners();
        }
        
        /// <summary>
        /// Wire up all event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            if (webSocketManager != null)
            {
                webSocketManager.OnGrammarCorrection += HandleGrammarCorrection;
                webSocketManager.OnTurnComplete += HandleTurnComplete;
                webSocketManager.OnTranscription += HandleTranscription;
                webSocketManager.OnConnected += HandleConnected;
                webSocketManager.OnDisconnected += HandleDisconnected;
            }
            
            if (audioCaptureManager != null)
            {
                audioCaptureManager.OnAudioChunkReady += HandleAudioChunk;
                audioCaptureManager.OnRecordingStarted += HandleRecordingStarted;
                audioCaptureManager.OnRecordingStopped += HandleRecordingStopped;
            }
            
            if (audioPlaybackManager != null)
            {
                audioPlaybackManager.OnPlaybackStarted += HandlePlaybackStarted;
                audioPlaybackManager.OnPlaybackComplete += HandlePlaybackComplete;
            }
        }
        
        // ========== Public API ==========
        
        /// <summary>
        /// Start a conversation with an NPC
        /// </summary>
        public void StartConversation(string npcId)
        {
            if (isInConversation)
            {
                Debug.LogWarning("[ConversationController] Already in conversation");
                return;
            }
            
            currentNpcId = npcId;
            isInConversation = true;
            
            Debug.Log($"[ConversationController] Starting conversation with {npcId}");
            
            // Notify backend
            gameEventSync?.OnDialogueStarted(npcId);
            
            // Connect WebSocket
            webSocketManager?.Connect(npcId);
        }
        
        /// <summary>
        /// End the current conversation
        /// </summary>
        public void EndConversation()
        {
            if (!isInConversation)
                return;
            
            Debug.Log($"[ConversationController] Ending conversation");
            
            // Stop any recording
            audioCaptureManager?.StopRecording();
            
            // Stop playback
            audioPlaybackManager?.Stop();
            
            // Disconnect WebSocket
            webSocketManager?.Disconnect();
            
            // Notify backend
            gameEventSync?.OnDialogueEnded(currentNpcId);
            
            isInConversation = false;
            currentNpcId = null;
        }
        
        /// <summary>
        /// Start speaking (player input)
        /// </summary>
        public void StartSpeaking()
        {
            if (!isInConversation)
            {
                Debug.LogWarning("[ConversationController] Not in conversation");
                return;
            }
            
            Debug.Log("[ConversationController] Player started speaking");
            audioCaptureManager?.StartRecording();
        }
        
        /// <summary>
        /// Stop speaking (player input complete)
        /// </summary>
        public void StopSpeaking()
        {
            Debug.Log("[ConversationController] Player stopped speaking");
            audioCaptureManager?.StopRecording();
        }
        
        // ========== Event Handlers ==========
        
        private void HandleAudioChunk(byte[] audioData)
        {
            // Send audio chunk to backend via WebSocket
            webSocketManager?.SendAudioChunk(audioData);
        }
        
        private void HandleRecordingStarted()
        {
            Debug.Log("[ConversationController] Recording started");
        }
        
        private void HandleRecordingStopped()
        {
            Debug.Log("[ConversationController] Recording stopped, sending end utterance");
            webSocketManager?.SendEndUtterance();
        }
        
        private void HandleGrammarCorrection(GrammarCorrectionData correction)
        {
            Debug.Log($"[ConversationController] Grammar correction received: {correction.original} → {correction.correction}");
            grammarFeedbackUI?.ShowCorrection(correction);
        }
        
        private void HandleTurnComplete()
        {
            Debug.Log("[ConversationController] NPC turn complete - player can speak again");
            // You could enable a UI button or indication here
        }
        
        private void HandleTranscription(string text)
        {
            Debug.Log($"[ConversationController] Transcription: {text}");
            // You could display this in a subtitle UI
        }
        
        private void HandleConnected()
        {
            Debug.Log("[ConversationController] WebSocket connected - ready for conversation");
        }
        
        private void HandleDisconnected()
        {
            Debug.Log("[ConversationController] WebSocket disconnected");
            isInConversation = false;
        }
        
        private void HandlePlaybackStarted()
        {
            Debug.Log("[ConversationController] NPC speaking...");
            // Disable player input during NPC speech if needed
        }
        
        private void HandlePlaybackComplete()
        {
            Debug.Log("[ConversationController] NPC finished speaking");
            // Re-enable player input
        }
        
        void OnDestroy()
        {
            if (isInConversation)
            {
                EndConversation();
            }
        }
    }
}
