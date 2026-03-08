using UnityEngine;
using VirtuLingo.Backend;

namespace VirtuLingo.Examples
{
    /// <summary>
    /// Simplified conversation controller for a single NPC setup
    /// Just press F to talk - no need to select NPCs
    /// </summary>
    public class SimplePushToTalkController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string npcId = "baker_01";
        
        [Header("Components")]
        [SerializeField] private ConversationController conversationController;
        [SerializeField] private GameEventSyncManager gameEventSync;
        
        [Header("UI")]
        [SerializeField] private GameObject pushToTalkIndicator; // "Hold F to talk"
        [SerializeField] private GameObject listeningIndicator;  // "Listening..."
        [SerializeField] private GameObject npcSpeakingIndicator; // "NPC Speaking..."
        
        [Header("Input")]
        [SerializeField] private KeyCode talkKey = KeyCode.F;
        [SerializeField] private KeyCode endConversationKey = KeyCode.Escape;
        
        [Header("Auto-Connect Settings")]
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private float autoConnectDelay = 1f;
        
        private bool isConnected = false;
        private bool isSpeaking = false;
        
        void Start()
        {
            // Find components if not assigned
            if (conversationController == null)
                conversationController = FindObjectOfType<ConversationController>();
            
            if (gameEventSync == null)
                gameEventSync = FindObjectOfType<GameEventSyncManager>();
            
            // Setup UI state
            SetUIState(UIState.Disconnected);
            
            // Auto-connect if enabled
            if (autoConnectOnStart)
            {
                Invoke(nameof(ConnectToNPC), autoConnectDelay);
            }
        }
        
        void Update()
        {
            if (!isConnected)
            {
                // Press F to connect
                if (Input.GetKeyDown(talkKey))
                {
                    ConnectToNPC();
                }
                return;
            }
            
            // Push-to-talk: Hold F to speak
            if (Input.GetKeyDown(talkKey) && !isSpeaking)
            {
                StartSpeaking();
            }
            
            if (Input.GetKeyUp(talkKey) && isSpeaking)
            {
                StopSpeaking();
            }
            
            // Press Escape to end conversation
            if (Input.GetKeyDown(endConversationKey))
            {
                DisconnectFromNPC();
            }
        }
        
        /// <summary>
        /// Connect to NPC (opens WebSocket, ready for conversation)
        /// </summary>
        public void ConnectToNPC()
        {
            if (isConnected)
            {
                Debug.LogWarning("[PushToTalk] Already connected");
                return;
            }
            
            Debug.Log($"[PushToTalk] Connecting to NPC: {npcId}");
            
            // Start conversation
            conversationController.StartConversation(npcId);
            
            // Notify backend
            gameEventSync?.OnDialogueStarted(npcId);
            
            isConnected = true;
            SetUIState(UIState.Ready);
        }
        
        /// <summary>
        /// Disconnect from NPC
        /// </summary>
        public void DisconnectFromNPC()
        {
            if (!isConnected)
                return;
            
            Debug.Log("[PushToTalk] Disconnecting from NPC");
            
            // Stop any ongoing speech
            if (isSpeaking)
            {
                StopSpeaking();
            }
            
            // End conversation
            conversationController.EndConversation();
            
            // Notify backend
            gameEventSync?.OnDialogueEnded(npcId);
            
            isConnected = false;
            SetUIState(UIState.Disconnected);
        }
        
        /// <summary>
        /// Start speaking (player input)
        /// </summary>
        private void StartSpeaking()
        {
            Debug.Log("[PushToTalk] Started speaking");
            
            conversationController.StartSpeaking();
            isSpeaking = true;
            SetUIState(UIState.PlayerSpeaking);
        }
        
        /// <summary>
        /// Stop speaking (player finished)
        /// </summary>
        private void StopSpeaking()
        {
            Debug.Log("[PushToTalk] Stopped speaking");
            
            conversationController.StopSpeaking();
            isSpeaking = false;
            SetUIState(UIState.WaitingForNPC);
        }
        
        /// <summary>
        /// UI state management
        /// </summary>
        private enum UIState
        {
            Disconnected,    // Not connected to NPC
            Ready,           // Connected, ready to talk
            PlayerSpeaking,  // Player is speaking
            WaitingForNPC,   // Waiting for NPC response
            NPCSpeaking      // NPC is speaking
        }
        
        private void SetUIState(UIState state)
        {
            // Hide all indicators
            if (pushToTalkIndicator) pushToTalkIndicator.SetActive(false);
            if (listeningIndicator) listeningIndicator.SetActive(false);
            if (npcSpeakingIndicator) npcSpeakingIndicator.SetActive(false);
            
            // Show appropriate indicator
            switch (state)
            {
                case UIState.Disconnected:
                    // Show nothing or "Press F to start"
                    break;
                
                case UIState.Ready:
                    if (pushToTalkIndicator)
                        pushToTalkIndicator.SetActive(true);
                    break;
                
                case UIState.PlayerSpeaking:
                    if (listeningIndicator)
                        listeningIndicator.SetActive(true);
                    break;
                
                case UIState.WaitingForNPC:
                case UIState.NPCSpeaking:
                    if (npcSpeakingIndicator)
                        npcSpeakingIndicator.SetActive(true);
                    break;
            }
        }
        
        /// <summary>
        /// Call this from ConversationController events
        /// </summary>
        public void OnNPCStartedSpeaking()
        {
            SetUIState(UIState.NPCSpeaking);
        }
        
        public void OnNPCFinishedSpeaking()
        {
            SetUIState(UIState.Ready);
        }
        
        void OnDestroy()
        {
            if (isConnected)
            {
                DisconnectFromNPC();
            }
        }
    }
}
