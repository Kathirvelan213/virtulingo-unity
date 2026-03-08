using System;
using System.Collections;
using UnityEngine;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Captures microphone input and converts to PCM format for backend
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioCaptureManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig config;
        
        [Header("Recording Settings")]
        [SerializeField] private int recordingLengthSec = 10;
        [SerializeField] private bool autoDetectSpeechEnd = true;
        [SerializeField] private float silenceThreshold = 0.01f;
        [SerializeField] private float silenceDuration = 1.5f;
        
        [Header("Status")]
        [SerializeField] private bool isRecording = false;
        
        // Events
        public event Action<byte[]> OnAudioChunkReady;
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;
        
        private AudioClip recordingClip;
        private AudioSource audioSource;
        private int lastSamplePosition = 0;
        private float silenceTimer = 0f;
        private string micDevice;
        
        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            micDevice = config.microphoneDevice;
            
            // If no device specified, use default
            if (string.IsNullOrEmpty(micDevice) && Microphone.devices.Length > 0)
            {
                micDevice = Microphone.devices[0];
                Debug.Log($"[AudioCapture] Using microphone: {micDevice}");
            }
        }
        
        /// <summary>
        /// Start recording from microphone
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("[AudioCapture] Already recording");
                return;
            }
            
            if (string.IsNullOrEmpty(micDevice))
            {
                Debug.LogError("[AudioCapture] No microphone device available");
                return;
            }
            
            Debug.Log($"[AudioCapture] Starting recording at {config.sampleRate}Hz");
            
            // Start microphone capture
            recordingClip = Microphone.Start(
                micDevice, 
                true, 
                recordingLengthSec, 
                config.sampleRate
            );
            
            isRecording = true;
            lastSamplePosition = 0;
            silenceTimer = 0f;
            
            OnRecordingStarted?.Invoke();
            
            // Start coroutine to process audio chunks
            StartCoroutine(ProcessAudioChunks());
        }
        
        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
                return;
            
            Debug.Log("[AudioCapture] Stopping recording");
            
            Microphone.End(micDevice);
            isRecording = false;
            
            OnRecordingStopped?.Invoke();
        }
        
        /// <summary>
        /// Process and send audio chunks while recording
        /// </summary>
        private IEnumerator ProcessAudioChunks()
        {
            while (isRecording)
            {
                int currentPosition = Microphone.GetPosition(micDevice);
                
                if (currentPosition < 0 || !Microphone.IsRecording(micDevice))
                {
                    Debug.LogWarning("[AudioCapture] Microphone stopped unexpectedly");
                    StopRecording();
                    yield break;
                }
                
                // Calculate samples available
                int samplesAvailable = currentPosition - lastSamplePosition;
                if (samplesAvailable < 0)
                {
                    // Wrapped around
                    samplesAvailable = recordingClip.samples - lastSamplePosition + currentPosition;
                }
                
                // Process in chunks
                if (samplesAvailable >= config.chunkSize)
                {
                    float[] samples = new float[config.chunkSize];
                    recordingClip.GetData(samples, lastSamplePosition);
                    
                    // Convert to PCM16 bytes
                    byte[] pcmData = ConvertToPCM16(samples);
                    
                    // Send to WebSocket
                    OnAudioChunkReady?.Invoke(pcmData);
                    
                    // Check for silence
                    if (autoDetectSpeechEnd && IsSilent(samples))
                    {
                        silenceTimer += Time.deltaTime;
                        if (silenceTimer >= silenceDuration)
                        {
                            Debug.Log("[AudioCapture] Silence detected, stopping");
                            StopRecording();
                        }
                    }
                    else
                    {
                        silenceTimer = 0f;
                    }
                    
                    // Update position
                    lastSamplePosition += config.chunkSize;
                    if (lastSamplePosition >= recordingClip.samples)
                    {
                        lastSamplePosition -= recordingClip.samples;
                    }
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Convert float samples to PCM16 format (backend expects this)
        /// </summary>
        private byte[] ConvertToPCM16(float[] samples)
        {
            byte[] pcm = new byte[samples.Length * 2];
            
            for (int i = 0; i < samples.Length; i++)
            {
                short pcm16 = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(pcm16);
                pcm[i * 2] = bytes[0];
                pcm[i * 2 + 1] = bytes[1];
            }
            
            return pcm;
        }
        
        /// <summary>
        /// Check if audio samples are silent
        /// </summary>
        private bool IsSilent(float[] samples)
        {
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += Mathf.Abs(sample);
            }
            float average = sum / samples.Length;
            return average < silenceThreshold;
        }
        
        void OnDestroy()
        {
            if (isRecording)
            {
                StopRecording();
            }
        }
    }
}
