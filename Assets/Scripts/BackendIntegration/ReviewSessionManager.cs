using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Manages adaptive review sessions
    /// Checks every 15 minutes if a review should be triggered
    /// </summary>
    public class ReviewSessionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig config;
        
        [Header("Review Settings")]
        [SerializeField] private int checkIntervalMinutes = 15;
        [SerializeField] private string reviewSceneName = "ReviewScene";
        
        [Header("Status")]
        [SerializeField] private int activePlayMinutes = 0;
        [SerializeField] private float sessionStartTime;
        
        // Events
        public event Action<ReviewSession> OnReviewSessionReady;
        public event Action OnReviewTriggered;
        
        private ReviewSession currentSession;
        private bool isCheckingReview = false;
        
        void Start()
        {
            sessionStartTime = Time.time;
            StartCoroutine(ReviewCheckTimer());
        }
        
        /// <summary>
        /// Timer that checks for review sessions every N minutes
        /// </summary>
        private IEnumerator ReviewCheckTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkIntervalMinutes * 60f);
                
                // Update active play time
                activePlayMinutes += checkIntervalMinutes;
                
                Debug.Log($"[ReviewSession] Checking review trigger at {activePlayMinutes} active minutes");
                
                CheckReviewTrigger();
            }
        }
        
        /// <summary>
        /// Check if review should be triggered
        /// </summary>
        public void CheckReviewTrigger()
        {
            if (isCheckingReview)
                return;
            
            StartCoroutine(CheckReviewCoroutine());
        }
        
        /// <summary>
        /// Manually trigger review generation
        /// </summary>
        public void ForceReview()
        {
            Debug.Log("[ReviewSession] Forcing review generation");
            StartCoroutine(GenerateReviewCoroutine());
        }
        
        /// <summary>
        /// Load the review scene with the current session
        /// </summary>
        public void LoadReviewScene()
        {
            if (currentSession == null)
            {
                Debug.LogWarning("[ReviewSession] No review session available");
                return;
            }
            
            OnReviewTriggered?.Invoke();
            
            // Store session data for the review scene to access
            PlayerPrefs.SetString("CurrentReviewSession", JsonUtility.ToJson(currentSession));
            PlayerPrefs.Save();
            
            // Load review scene
            SceneManager.LoadScene(reviewSceneName);
        }
        
        // ========== HTTP Coroutines ==========
        
        private IEnumerator CheckReviewCoroutine()
        {
            isCheckingReview = true;
            
            string url = config.GetReviewCheckUrl();
            
            var request = new ReviewCheckRequest
            {
                player_id = config.playerId,
                active_minutes = activePlayMinutes
            };
            
            string json = JsonUtility.ToJson(request);
            
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = webRequest.downloadHandler.text;
                    var response = JsonUtility.FromJson<ReviewCheckResponse>(responseJson);
                    
                    Debug.Log($"[ReviewSession] Review check result: trigger={response.trigger_review}");
                    
                    if (response.trigger_review && response.session != null)
                    {
                        currentSession = response.session;
                        OnReviewSessionReady?.Invoke(currentSession);
                        
                        // Optionally auto-load review scene
                        // LoadReviewScene();
                    }
                }
                else
                {
                    Debug.LogError($"[ReviewSession] Error checking review: {webRequest.error}");
                }
            }
            
            isCheckingReview = false;
        }
        
        private IEnumerator GenerateReviewCoroutine()
        {
            string url = $"{config.HttpProtocol}://{config.serverUrl}/api/review/{config.playerId}/generate";
            
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = webRequest.downloadHandler.text;
                    currentSession = JsonUtility.FromJson<ReviewSession>(responseJson);
                    
                    Debug.Log($"[ReviewSession] Review session generated with {currentSession.exercises.Count} exercises");
                    
                    OnReviewSessionReady?.Invoke(currentSession);
                }
                else
                {
                    Debug.LogError($"[ReviewSession] Error generating review: {webRequest.error}");
                }
            }
        }
        
        /// <summary>
        /// Get the current review session (for review scene)
        /// </summary>
        public static ReviewSession GetStoredSession()
        {
            string json = PlayerPrefs.GetString("CurrentReviewSession", "");
            if (string.IsNullOrEmpty(json))
                return null;
            
            return JsonUtility.FromJson<ReviewSession>(json);
        }
    }
}
