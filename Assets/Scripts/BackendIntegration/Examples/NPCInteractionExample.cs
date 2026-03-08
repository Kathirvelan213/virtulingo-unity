using UnityEngine;
using VirtuLingo.Backend;

namespace VirtuLingo.Examples
{
    /// <summary>
    /// Example script showing how to integrate NPC interactions with the backend system
    /// Attach this to your NPC GameObjects or trigger areas
    /// </summary>
    public class NPCInteractionExample : MonoBehaviour
    {
        [Header("NPC Data")]
        [SerializeField] private string npcId = "baker_01";
        [SerializeField] private string npcName = "Pierre the Baker";
        
        [Header("UI")]
        [SerializeField] private GameObject interactionPrompt; // "Press E to talk"
        [SerializeField] private GameObject talkIndicator;     // "Hold F to talk"
        
        [Header("Settings")]
        [SerializeField] private float interactionRadius = 3f;
        
        private ConversationController conversationController;
        private GameEventSyncManager gameEventSync;
        private Transform playerTransform;
        private bool playerInRange = false;
        private bool isInConversation = false;
        
        void Start()
        {
            // Find managers (assuming they're in the scene)
            conversationController = FindObjectOfType<ConversationController>();
            gameEventSync = FindObjectOfType<GameEventSyncManager>();
            
            // Find player
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Hide UI initially
            if (interactionPrompt) interactionPrompt.SetActive(false);
            if (talkIndicator) talkIndicator.SetActive(false);
        }
        
        void Update()
        {
            if (playerTransform == null)
                return;
            
            // Check if player is in range
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = distance <= interactionRadius;
            
            if (inRange && !playerInRange)
            {
                OnPlayerEnterRange();
            }
            else if (!inRange && playerInRange)
            {
                OnPlayerExitRange();
            }
            
            playerInRange = inRange;
            
            // Handle interaction
            if (playerInRange && !isInConversation)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    StartNPCConversation();
                }
            }
            
            // Handle talking (during conversation)
            if (isInConversation)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    conversationController.StartSpeaking();
                }
                
                if (Input.GetKeyUp(KeyCode.F))
                {
                    conversationController.StopSpeaking();
                }
                
                // End conversation
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    EndNPCConversation();
                }
            }
        }
        
        /// <summary>
        /// Called when player enters interaction range
        /// </summary>
        private void OnPlayerEnterRange()
        {
            Debug.Log($"[NPCInteraction] Player entered range of {npcName}");
            
            // Show interaction prompt
            if (interactionPrompt)
                interactionPrompt.SetActive(true);
            
            // Notify backend
            gameEventSync?.OnEnteredNPCProximity(npcId);
        }
        
        /// <summary>
        /// Called when player exits interaction range
        /// </summary>
        private void OnPlayerExitRange()
        {
            Debug.Log($"[NPCInteraction] Player left range of {npcName}");
            
            // Hide interaction prompt
            if (interactionPrompt)
                interactionPrompt.SetActive(false);
            
            // End conversation if active
            if (isInConversation)
            {
                EndNPCConversation();
            }
            
            // Notify backend
            gameEventSync?.OnLeftNPCProximity(npcId);
        }
        
        /// <summary>
        /// Start conversation with this NPC
        /// </summary>
        public void StartNPCConversation()
        {
            if (conversationController == null)
            {
                Debug.LogError("[NPCInteraction] ConversationController not found!");
                return;
            }
            
            Debug.Log($"[NPCInteraction] Starting conversation with {npcName}");
            
            isInConversation = true;
            
            // Hide interaction prompt, show talk indicator
            if (interactionPrompt) interactionPrompt.SetActive(false);
            if (talkIndicator) talkIndicator.SetActive(true);
            
            // Start conversation (connects WebSocket, notifies backend)
            conversationController.StartConversation(npcId);
            
            // Optional: Disable player movement
            // DisablePlayerMovement();
            
            // Optional: Play NPC greeting animation
            // GetComponent<Animator>()?.SetTrigger("Greet");
        }
        
        /// <summary>
        /// End conversation with this NPC
        /// </summary>
        public void EndNPCConversation()
        {
            if (!isInConversation)
                return;
            
            Debug.Log($"[NPCInteraction] Ending conversation with {npcName}");
            
            isInConversation = false;
            
            // Hide talk indicator, show interaction prompt if still in range
            if (talkIndicator) talkIndicator.SetActive(false);
            if (interactionPrompt && playerInRange) interactionPrompt.SetActive(true);
            
            // End conversation (disconnects WebSocket, notifies backend)
            conversationController.EndConversation();
            
            // Optional: Re-enable player movement
            // EnablePlayerMovement();
            
            // Optional: Play NPC goodbye animation
            // GetComponent<Animator>()?.SetTrigger("Wave");
        }
        
        // ========== Visualize Interaction Range in Editor ==========
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
