using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Displays grammar corrections from the backend
    /// </summary>
    public class GrammarFeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI originalText;
        [SerializeField] private TextMeshProUGUI correctionText;
        [SerializeField] private TextMeshProUGUI explanationText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Image severityIndicator;
        
        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 5f;
        [SerializeField] private bool autoHide = true;
        
        [Header("Severity Colors")]
        [SerializeField] private Color severity1Color = Color.green;
        [SerializeField] private Color severity2Color = Color.yellow;
        [SerializeField] private Color severity3Color = new Color(1f, 0.5f, 0f); // orange
        [SerializeField] private Color severity4Color = new Color(1f, 0.3f, 0f); // red-orange
        [SerializeField] private Color severity5Color = Color.red;
        
        private Queue<GrammarCorrectionData> correctionQueue = new Queue<GrammarCorrectionData>();
        private bool isDisplaying = false;
        
        void Start()
        {
            if (feedbackPanel != null)
            {
                // Safety check: if feedbackPanel IS this GameObject or one of its ancestors,
                // calling SetActive(false) would disable this script too — breaking all future callbacks.
                // This happens when the Inspector reference is set to the root panel that also holds this script.
                if (transform.IsChildOf(feedbackPanel.transform))
                {
                    Debug.LogWarning("[GrammarFeedbackUI] 'Feedback Panel' is set to this GameObject or a parent of it. " +
                                     "The panel must be a CHILD of the GameObject this script is on, not the same object or a parent. " +
                                     "Please reassign it in the Inspector.");
                }
                else
                {
                    feedbackPanel.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Show grammar correction
        /// </summary>
        public void ShowCorrection(GrammarCorrectionData correction)
        {
            if (!correction.mistake_found)
            {
                Debug.Log("[GrammarFeedback] No mistake found, skipping display");
                return;
            }
            
            correctionQueue.Enqueue(correction);
            
            if (!isDisplaying)
            {
                ShowNextCorrection();
            }
        }
        
        /// <summary>
        /// Show the next correction in queue
        /// </summary>
        private void ShowNextCorrection()
        {
            if (correctionQueue.Count == 0)
            {
                isDisplaying = false;
                return;
            }
            
            isDisplaying = true;
            var correction = correctionQueue.Dequeue();
            
            // Update UI
            if (originalText != null)
                originalText.text = $"❌ {correction.original}";
            
            if (correctionText != null)
                correctionText.text = $"✓ {correction.correction}";
            
            if (explanationText != null)
                explanationText.text = correction.explanation;
            
            if (categoryText != null)
                categoryText.text = FormatCategory(correction.category);
            
            if (severityIndicator != null)
                severityIndicator.color = GetSeverityColor(correction.severity);
            
            // Show panel
            if (feedbackPanel != null)
                feedbackPanel.SetActive(true);
            
            Debug.Log($"[GrammarFeedback] Displaying correction: {correction.original} → {correction.correction}");
            
            // Auto-hide after duration
            if (autoHide)
            {
                StartCoroutine(HideAfterDelay());
            }
        }
        
        /// <summary>
        /// Hide feedback panel after delay
        /// </summary>
        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            HidePanel();
            
            // Show next correction if any
            yield return new WaitForSeconds(0.5f);
            ShowNextCorrection();
        }
        
        /// <summary>
        /// Manually hide the panel
        /// </summary>
        public void HidePanel()
        {
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Format category name for display
        /// </summary>
        private string FormatCategory(string category)
        {
            return category switch
            {
                "verb_conjugation" => "Verb Conjugation",
                "gender_agreement" => "Gender Agreement",
                "tense" => "Tense",
                "vocabulary" => "Vocabulary",
                "pronunciation_flag" => "Pronunciation",
                _ => category
            };
        }
        
        /// <summary>
        /// Get color based on severity (1-5)
        /// </summary>
        private Color GetSeverityColor(int severity)
        {
            return severity switch
            {
                1 => severity1Color,
                2 => severity2Color,
                3 => severity3Color,
                4 => severity4Color,
                5 => severity5Color,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Clear all queued corrections
        /// </summary>
        public void ClearQueue()
        {
            correctionQueue.Clear();
            HidePanel();
            isDisplaying = false;
        }
    }
}
