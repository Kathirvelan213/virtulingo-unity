using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Manages playback of audio chunks received from backend (NPC voice)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlaybackManager : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private int queuedChunks = 0;
        
        // Events
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackComplete;
        
        private AudioSource audioSource;
        private Queue<byte[]> audioChunkQueue = new Queue<byte[]>();
        private List<byte[]> currentUtteranceChunks = new List<byte[]>();
        private bool isProcessing = false;
        
        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;
        }
        
        /// <summary>
        /// Queue an audio chunk for playback
        /// </summary>
        public void QueueAudioChunk(byte[] audioData)
        {
            audioChunkQueue.Enqueue(audioData);
            queuedChunks = audioChunkQueue.Count;
            
            Debug.Log($"[AudioPlayback] Queued chunk: {audioData.Length} bytes. Queue size: {queuedChunks}");
            
            if (!isProcessing)
            {
                ProcessQueue();
            }
        }
        
        /// <summary>
        /// Process queued audio chunks
        /// </summary>
        private void ProcessQueue()
        {
            if (audioChunkQueue.Count == 0)
            {
                isProcessing = false;
                return;
            }
            
            isProcessing = true;
            
            // Collect all chunks for current utterance
            currentUtteranceChunks.Clear();
            while (audioChunkQueue.Count > 0)
            {
                currentUtteranceChunks.Add(audioChunkQueue.Dequeue());
            }
            
            queuedChunks = 0;
            
            Debug.Log($"[AudioPlayback] Processing {currentUtteranceChunks.Count} WAV chunks");
            
            // Each chunk is a complete WAV file - extract and combine PCM data
            List<float> combinedPcmData = new List<float>();
            int channels = 0;
            int sampleRate = 0;
            
            foreach (var wavChunk in currentUtteranceChunks)
            {
                var pcmData = ExtractPcmFromWav(wavChunk, out int chunkChannels, out int chunkSampleRate);
                
                if (pcmData != null)
                {
                    // Store first chunk's audio format
                    if (channels == 0)
                    {
                        channels = chunkChannels;
                        sampleRate = chunkSampleRate;
                    }
                    
                    combinedPcmData.AddRange(pcmData);
                    Debug.Log($"[AudioPlayback] Extracted {pcmData.Length} PCM samples from chunk ({pcmData.Length / chunkChannels / chunkSampleRate:F2}s)");
                }
                else
                {
                    Debug.LogError("[AudioPlayback] Failed to extract PCM from WAV chunk");
                }
            }
            
            if (combinedPcmData.Count > 0 && channels > 0)
            {
                Debug.Log($"[AudioPlayback] Combined {combinedPcmData.Count} total PCM samples ({combinedPcmData.Count / channels / sampleRate:F2}s)");
                PlayPcmData(combinedPcmData.ToArray(), channels, sampleRate);
            }
            else
            {
                Debug.LogError("[AudioPlayback] No valid PCM data extracted from chunks");
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Play PCM audio data directly
        /// </summary>
        private void PlayPcmData(float[] pcmData, int channels, int sampleRate)
        {
            try
            {
                Debug.Log($"[AudioPlayback] PlayPcmData: {pcmData.Length} samples, {channels}ch, {sampleRate}Hz");
                
                // Check AudioSource
                if (audioSource == null)
                {
                    Debug.LogError("[AudioPlayback] AudioSource is null! Assign it in Inspector.");
                    isProcessing = false;
                    return;
                }
                
                Debug.Log($"[AudioPlayback] AudioSource: volume={audioSource.volume}, mute={audioSource.mute}, enabled={audioSource.enabled}");
                
                // Create AudioClip from PCM data
                int sampleCount = pcmData.Length / channels;
                AudioClip clip = AudioClip.Create("NPCAudio", sampleCount, channels, sampleRate, false);
                
                if (!clip.SetData(pcmData, 0))
                {
                    Debug.LogError("[AudioPlayback] Failed to set audio data on clip");
                    isProcessing = false;
                    return;
                }
                
                Debug.Log($"[AudioPlayback] AudioClip created: {clip.length:F2}s, {clip.channels}ch, {clip.frequency}Hz");
                
                isPlaying = true;
                OnPlaybackStarted?.Invoke();
                
                audioSource.clip = clip;
                audioSource.Play();
                
                Debug.Log($"[AudioPlayback] Playing audio (isPlaying={audioSource.isPlaying})");
                
                // Schedule playback complete callback
                Invoke(nameof(OnPlaybackFinished), clip.length);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioPlayback] Error playing audio: {e.Message}");
                Debug.LogError($"[AudioPlayback] Stack trace: {e.StackTrace}");
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Called when playback finishes
        /// </summary>
        private void OnPlaybackFinished()
        {
            isPlaying = false;
            OnPlaybackComplete?.Invoke();
            
            Debug.Log("[AudioPlayback] Playback complete");
            
            // Process any new chunks that arrived
            if (audioChunkQueue.Count > 0)
            {
                ProcessQueue();
            }
            else
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Extract PCM audio data from a WAV file byte array
        /// </summary>
        /// <returns>PCM samples as float array (-1.0 to 1.0), or null on error</returns>
        private float[] ExtractPcmFromWav(byte[] wavBytes, out int channels, out int sampleRate)
        {
            channels = 0;
            sampleRate = 0;
            
            try
            {
                if (wavBytes.Length < 44)
                {
                    Debug.LogError($"[AudioPlayback] WAV too short: {wavBytes.Length} bytes (need at least 44)");
                    return null;
                }
                
                // Verify RIFF header
                string riff = System.Text.Encoding.ASCII.GetString(wavBytes, 0, 4);
                if (riff != "RIFF")
                {
                    Debug.LogError($"[AudioPlayback] Invalid WAV: missing RIFF header (got '{riff}')");
                    return null;
                }
                
                // Parse WAV header
                channels = BitConverter.ToInt16(wavBytes, 22);
                sampleRate = BitConverter.ToInt32(wavBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(wavBytes, 34);
                
                // For standard WAV files from Azure TTS, data chunk starts at byte 44
                // Format: RIFF header (12 bytes) + fmt chunk (24 bytes) + data chunk header (8 bytes) = 44 bytes
                int dataOffset = 44;
                
                // Verify we have enough data
                if (dataOffset >= wavBytes.Length)
                {
                    Debug.LogError($"[AudioPlayback] WAV file too short for data (length={wavBytes.Length}, need >{dataOffset})");
                    return null;
                }
                
                // Calculate data size from file length
                int dataSize = wavBytes.Length - dataOffset;
                int sampleCount = dataSize / (bitsPerSample / 8);
                
                Debug.Log($"[AudioPlayback] WAV: {channels}ch, {sampleRate}Hz, {bitsPerSample}bit, {dataSize} bytes, {sampleCount} samples");
                
                if (channels == 0 || sampleRate == 0)
                {
                    Debug.LogError($"[AudioPlayback] Invalid WAV: channels={channels}, sampleRate={sampleRate}");
                    return null;
                }
                
                // Convert to float samples
                float[] pcmData = new float[sampleCount];
                
                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = BitConverter.ToInt16(wavBytes, dataOffset + i * 2);
                        pcmData[i] = sample / 32768f;
                    }
                }
                else
                {
                    Debug.LogError($"[AudioPlayback] Unsupported bits per sample: {bitsPerSample} (expected 16)");
                    return null;
                }
                
                return pcmData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioPlayback] Failed to extract PCM: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Stop current playback
        /// </summary>
        public void Stop()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            audioChunkQueue.Clear();
            currentUtteranceChunks.Clear();
            isPlaying = false;
            isProcessing = false;
            queuedChunks = 0;
            
            CancelInvoke(nameof(OnPlaybackFinished));
        }
    }
}
