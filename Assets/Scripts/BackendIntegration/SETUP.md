# VirtuLingo Unity Setup Guide

Complete setup guide for integrating VirtuLingo backend with Unity. This covers voice chat, zone-based location tracking, and push-to-talk controls.

---

## 📋 Prerequisites

- Unity 2020.3 or later
- Backend server running at `http://localhost:8000`
- NativeWebSocket package
- TextMeshPro package (for UI)

---

## 🚀 Quick Start (5 Minutes)

### 1. Install Required Packages

**NativeWebSocket:**
1. Window → Package Manager
2. Click `+` → Add package from git URL
3. Enter: `https://github.com/endel/NativeWebSocket.git#upm`

**TextMeshPro:**
- Unity should prompt on first use, or Window → TextMeshPro → Import TMP Essential Resources

### 2. Create Backend Configuration

1. Right-click in Project window → Create → VirtuLingo → Backend Config
2. Name it `VirtuLingoConfig`
3. Set values:
   - Server URL: `localhost:8000`
   - Player ID: `player123` (or your ID)
   - Sample Rate: `16000`

### 3. Setup Scene

#### A. Create ConversationManager

1. Create empty GameObject: `ConversationManager`
2. Add these components:
   - `ConversationController`
   - `ConversationWebSocketManager`
   - `AudioCaptureManager`
   - `AudioPlaybackManager` + AudioSource
   - `GameEventSyncManager`
   - `GrammarFeedbackUI`
   - `ReviewSessionManager`

3. Configure the components:

   **On ConversationController:**
   - Web Socket Manager → drag `ConversationWebSocketManager` component (from same GameObject)
   - Audio Capture Manager → drag `AudioCaptureManager` component
   - Audio Playback Manager → drag `AudioPlaybackManager` component
   - Game Event Sync → drag `GameEventSyncManager` component
   - Grammar Feedback UI → drag `GrammarFeedbackUI` component
   
   **On ConversationWebSocketManager:**
   - Config → drag `VirtuLingoConfig` asset
   
   **On AudioCaptureManager:**
   - Config → drag `VirtuLingoConfig` asset
   
   **On GameEventSyncManager:**
   - Config → drag `VirtuLingoConfig` asset
   
   **On ReviewSessionManager:**
   - Config → drag `VirtuLingoConfig` asset
   
   **On AudioPlaybackManager:**
   - Nothing needed (uses AudioSource automatically)

#### B. Setup Player with Zones

1. On your **Player** GameObject:
   - Tag: `Player`
   - Add: `LocationZoneManager`
   - Wire: Game Event Sync → drag from ConversationManager

2. Configure zones (see Zone Setup section below)

#### C. Create Push-to-Talk Controller

1. Create GameObject: `PushToTalkController`
2. Add component: `SimplePushToTalkController`
3. Configure:
   - NPC ID: `baker_01` (your NPC's ID)
   - Conversation Controller → drag from ConversationManager
   - Game Event Sync → drag from ConversationManager
   - Auto Connect On Start: ✓

#### D. Create Grammar Feedback UI

1. Create Canvas (if you don't have one)
2. Create UI → Panel, name it `GrammarFeedbackPanel`
3. Inside the panel, create these TextMeshProUGUI elements:
   - `CategoryText` (e.g., "Verb Conjugation")
   - `OriginalText` (your incorrect sentence)
   - `CorrectionText` (the corrected version)
   - `ExplanationText` (why it was wrong)
4. Optionally add an Image for `SeverityIndicator`
5. Wire to GrammarFeedbackUI component:
   - Feedback Panel → the panel GameObject
   - Original Text → the original TMP text
   - Correction Text → the correction TMP text
   - Explanation Text → the explanation TMP text
   - Category Text → the category TMP text
   - Severity Indicator → the Image (optional)

### 4. Test

1. Start backend: `uvicorn main:app --reload`
2. Press Play in Unity
3. Hold **F** and speak
4. Release to hear NPC response

---

## 🗺️ Zone-Based Location Setup

### Why Zones?

**Old Way (Coordinates):** ❌
- Unity sends: `{x: 45.2, y: 0, z: 122.7}`
- Backend: "Player at position 45, 0, 122"
- NPC AI: "What does that mean? 🤷"

**New Way (Zones):** ✅
- Unity sends: `scene_id = "bakery"`
- Backend: "Player is in the bakery"
- NPC AI: "Perfect! I can talk about bread and pastries!"

### Visual Layout

```
┌─────────────────────────────────────────────┐
│         YOUR UNITY SCENE (Top View)         │
│                                             │
│  ┌──────────────┐                          │
│  │   BAKERY     │  ← Box Collider Zone     │
│  │   (Trigger)  │     ID: "bakery"         │
│  │    [Baker]   │                          │
│  └──────────────┘                          │
│                                             │
│         ┌────────────────┐                 │
│         │  MARKETPLACE   │                 │
│         │   (Trigger)    │ ← ID: "marketplace"
│         │                │                 │
│         └────────────────┘                 │
│                                             │
│  ┌──────────────┐                          │
│  │     CAFÉ     │  ← Box Collider Zone     │
│  │   (Trigger)  │     ID: "cafe"           │
│  │   [Tables]   │                          │
│  └──────────────┘                          │
│                                             │
│         👤 ← Player walks around            │
└─────────────────────────────────────────────┘
```

### Step 1: Create Zone Colliders

For each zone (bakery, marketplace, cafe):

1. Create empty GameObject (e.g., `Bakery_Zone`)
2. Add Component → Box Collider
3. Configure collider:
   - ✓ **Is Trigger** (IMPORTANT!)
   - Size: Scale to cover floor area
4. Position zone over the area

### Step 2: Configure LocationZoneManager

On your **Player** GameObject with LocationZoneManager:

```
Zones: Size = 3

Element 0:
├─ Zone Id: bakery
├─ Display Name: The Bakery
├─ Zone Trigger: [Bakery_Zone]  ← Drag the GameObject
└─ Description: A warm French bakery

Element 1:
├─ Zone Id: marketplace
├─ Display Name: Marketplace
├─ Zone Trigger: [Marketplace_Zone]
└─ Description: Open-air market

Element 2:
├─ Zone Id: cafe
├─ Display Name: Street Café
├─ Zone Trigger: [Cafe_Zone]
└─ Description: Cozy outdoor café
```

### Step 3: Verify Player Setup

Your Player GameObject needs:
- Tag: `Player` ← Zone detection uses this
- Collider (Capsule or Box) ← To trigger zones
- LocationZoneManager ← Zone tracking
- Character Controller or Rigidbody ← Movement

### How It Works

```
Player Enters Zone
    ↓
Zone Trigger (OnTriggerEnter)
    ↓
LocationZoneManager.EnterZone("bakery")
    ↓
GameEventSyncManager.OnSceneChanged("bakery")
    ↓
HTTP POST to /api/events/
    ↓
Backend updates player_state.scene_id = "bakery"
    ↓
Next conversation includes location context
```

### Backend Integration

When you enter a zone, backend receives:
```json
{
  "player_id": "player123",
  "event_type": "SceneChanged",
  "payload": {
    "scene_id": "bakery"
  }
}
```

NPC system prompt includes:
```
You are Jacques, a French baker.

WORLD CONTEXT:
- Scene: bakery
- Player is holding: baguette
- Active quest: learn_greetings

Respond naturally, mentioning your surroundings.
```

Result: **Context-aware conversations!**
- In bakery: "Bonjour! Looking for fresh bread?"
- In café: "I just delivered croissants here this morning!"

---

## 🎤 Push-to-Talk System

### How It Works

```
Player Action       Unity                Backend
──────────────────────────────────────────────────

Hold F        →  StartRecording()
                 ├─ Mic captures
                 └─ Stream audio    → STT transcribes

Keep holding  →  Streaming...       → Building text

Release F     →  StopRecording()
                 SendEndUtterance() → Complete!
                                      ├─ Grammar check
                                      ├─ Generate reply
                                      └─ Create audio

Listen        ←  Play audio        ← Stream response

See feedback  ←  Show grammar      ← Parallel check
```

### Configuration

SimplePushToTalkController inspector:
```
NPC ID: baker_01
Conversation Controller: [ConversationManager]
Game Event Sync: [ConversationManager]
Auto Connect On Start: ✓

Optional UI (drag TextMeshPro):
├─ Ready Indicator: "Hold F to talk"
└─ Recording Indicator: "🎤 Listening..."
```

### Optional: Add UI Indicators

1. Create TextMeshPro text: `ReadyText`
   - Text: "Hold F to talk to baker"
   - Color: White

2. Create TextMeshPro text: `RecordingText`
   - Text: "🎤 Listening..."
   - Color: Red
   - Active: OFF (will show when recording)

3. Wire to SimplePushToTalkController:
   - Ready Indicator → ReadyText
   - Recording Indicator → RecordingText

---

## 🎨 Grammar Feedback UI

### Setup

1. Create UI Panel for feedback container
2. Inside panel, add TextMeshPro texts for:
   - Category (header)
   - Original (your text)
   - Correction (fixed text)  
   - Explanation (why)
3. Optionally add Image for severity color indicator
4. Wire all to GrammarFeedbackUI component

### Severity Colors

The system shows corrections color-coded by severity:

| Severity | Color  | Meaning               |
|----------|--------|-----------------------|
| 1        | Green  | Minor/suggestion      |
| 2        | Yellow | Noticeable            |
| 3        | Orange | Important             |
| 4        | Red    | Serious error         |
| 5        | Purple | Critical/incomprehensible |

### Example Display

```
Verb Conjugation
✗ "Je parle français"
✓ "Je parles français"

You used the wrong verb form. "Parles" is second-person.
```

Auto-hides after 5 seconds (configurable in inspector).

---

## 📚 Complete Component Reference

### BackendConfig (ScriptableObject)

Create via: Create → VirtuLingo → Backend Config

| Field | Default | Description |
|-------|---------|-------------|
| Server URL | localhost:8000 | Backend server address |
| Player ID | player123 | Unique player identifier |
| Sample Rate | 16000 | Audio recording rate (Hz) |
| Use Secure WebSocket | false | Use wss:// instead of ws:// |
| Use HTTPS | false | Use https:// instead of http:// |

### ConversationController

Main orchestrator. Wire all other components here.

**Events to Subscribe:**
- `OnTranscriptionReceived(string)` - Player's transcribed speech
- `OnNPCResponseStarted()` - NPC starts talking
- `OnNPCResponseEnded()` - NPC finishes talking
- `OnGrammarCorrection(GrammarCorrectionData)` - Grammar feedback received

### ConversationWebSocketManager

Handles WebSocket connection for real-time voice chat.

**Public Methods:**
- `Connect(string playerId, string npcId)` - Connect to NPC
- `Disconnect()` - Close connection
- `SendAudioChunk(byte[] pcm16Data)` - Stream audio
- `SendEndUtterance()` - Signal speech ended

### AudioCaptureManager

Records microphone input and converts to PCM16.

| Field | Default | Description |
|-------|---------|-------------|
| Chunk Duration | 0.1s | How often to send audio |
| Silence Threshold | 0.01 | Auto-stop threshold |
| Silence Duration | 2.0s | Silence time before auto-stop |
| Enable Silence Detection | false | Auto-stop when silent |

**Public Methods:**
- `StartRecording()` - Begin recording
- `StopRecording()` - End recording

### AudioPlaybackManager

Plays NPC voice responses.

| Field | Default | Description |
|-------|---------|-------------|
| Volume | 1.0 | Playback volume |

**Public Methods:**
- `QueueAudioChunk(byte[] audioData)` - Add audio to queue
- `StopPlayback()` - Stop current audio

### GameEventSyncManager

Syncs game events to backend.

| Field | Default | Description |
|-------|---------|-------------|
| Enable Position Sync | false | Send position updates (prefer zones) |
| Position Sync Interval | 1.0s | How often to sync position |

**Public Methods:**
- `SendEvent(string eventType, object payload)` - Send custom event
- `OnSceneChanged(string sceneId)` - Player changed location
- `OnPickedObject(string objectId)` - Player picked up item
- `OnDroppedObject(string objectId)` - Player dropped item
- `OnQuestStarted(string questId)` - Quest started
- `OnQuestCompleted(string questId)` - Quest completed

### LocationZoneManager

Zone-based location tracking.

**Zone Configuration:**
- Zone ID: Unique identifier (e.g., "bakery")
- Display Name: Human-readable (e.g., "The Bakery")
- Zone Trigger: The trigger collider GameObject
- Description: Context for debugging

**Events:**
- `OnZoneChanged(string zoneId, string displayName)` - Zone entered

### SimplePushToTalkController

Single-NPC push-to-talk controls.

| Field | Default | Description |
|-------|---------|-------------|
| NPC ID | baker_01 | NPC to talk to |
| Auto Connect On Start | true | Connect on Start() |
| Talk Key | F | Key to hold for recording |

**Optional UI:**
- Ready Indicator: Text shown when ready
- Recording Indicator: Text shown while recording

### GrammarFeedbackUI

Display grammar corrections with detailed breakdown.

| Field | Default | Description |
|-------|---------|-------------|
| Feedback Panel | - | Container panel to show/hide |
| Original Text | - | Shows player's incorrect text |
| Correction Text | - | Shows corrected version |
| Explanation Text | - | Shows why it was wrong |
| Category Text | - | Shows error type (e.g., "Verb Conjugation") |
| Severity Indicator | - | Optional image to color-code severity |
| Display Duration | 5.0s | How long to show feedback |
| Auto Hide | true | Automatically hide after duration |

### ReviewSessionManager

Manages adaptive learning review sessions.

| Field | Default | Description |
|-------|---------|-------------|
| Check Interval | 15 min | How often to check for reviews |
| Review Scene Name | ReviewScene | Scene to load for reviews |
| Enable Auto Check | true | Automatically check for reviews |

---

## 🧪 Testing Your Setup

### Test Checklist

#### ✓ Backend Connection
1. Start backend: `uvicorn main:app --reload`
2. Press Play in Unity
3. Check console: `[ConversationWS] Connected!`

#### ✓ Zone Detection
1. Walk player into a zone
2. Check console: `[LocationZone] Entered zone: The Bakery (bakery)`
3. Check backend logs: Should show SceneChanged event

#### ✓ Push-to-Talk
1. Hold F key
2. Check console: `[PushToTalk] Started speaking`
3. Check console: `[AudioCapture] Recording started`
4. Release F
5. Check console: `[PushToTalk] Stopped speaking`

#### ✓ Audio Playback
1. After speaking, wait for response
2. Check console: `[ConversationWS] Received audio chunk`
3. Hear NPC voice audio

#### ✓ Grammar Feedback
1. Make a grammar mistake when speaking
2. Check UI: Grammar correction should appear
3. Check console: `[ConversationController] Grammar correction received`

### Console Logs to Watch

**Good Signs:** ✓
```
[ConversationWS] Connected!
[LocationZone] Entered zone: The Bakery (bakery)
[GameEventSync] Sending event: SceneChanged
[PushToTalk] Started speaking
[AudioCapture] Recording started
[ConversationWS] Sent audio chunk (3200 bytes)
[PushToTalk] Stopped speaking
[ConversationWS] Received audio chunk (4096 bytes)
[AudioPlayback] Playing audio chunk
[GrammarFeedback] Showing correction: Verb Conjugation
```

**Bad Signs:** ❌
```
[ConversationWS] Connection failed
→ Check backend is running

[LocationZone] No zone entered
→ Check zone collider "Is Trigger" is enabled
→ Check player tagged "Player"

[AudioCapture] No microphone found
→ Check system microphone permissions

[ConversationWS] WebSocket error
→ Check server URL in BackendConfig
```

---

## 🔧 Troubleshooting

### WebSocket Won't Connect

**Symptom:** `[ConversationWS] Connection failed`

**Solutions:**
1. Check backend is running: `http://localhost:8000/docs`
2. Verify URL in BackendConfig matches backend
3. Check firewall isn't blocking port 8000
4. Look at backend console for error messages

### Zones Not Detecting

**Symptom:** Walking through zones, no console messages

**Solutions:**
1. Player must have tag: `Player`
2. Player must have a Collider (Capsule/Box)
3. Zone colliders must have "Is Trigger" = ✓
4. LocationZoneManager needs zones configured
5. GameEventSyncManager must be wired up

### No Audio Recording

**Symptom:** Holding F, but no recording

**Solutions:**
1. Check microphone permissions in Windows
2. Verify Microphone.devices is not empty (log it)
3. Check AudioCaptureManager is on ConversationManager
4. Verify BackendConfig sample rate = 16000

### No Audio Playback

**Symptom:** No NPC voice heard

**Solutions:**
1. Check AudioSource is on ConversationManager
2. Verify AudioPlaybackManager volume > 0
3. Check AudioSource isn't muted
4. Look for `[ConversationWS] Received audio chunk` in console
5. Check backend is sending audio (check backend logs)

### Grammar Feedback Not Showing

**Symptom:** No corrections appear in UI

**Solutions:**
1. Verify GrammarFeedbackUI has panel and text wired
2. Check panel is enabled in hierarchy
3. Look for `[GrammarFeedback]` logs in console
4. Check canvas is rendering (Canvas → Screen Space Overlay)

### Backend Shows Wrong Zone

**Symptom:** Backend logs show different zone than player is in

**Solutions:**
1. Check GameEventSyncManager is wired to LocationZoneManager
2. Verify zone IDs match between Unity and backend expectations
3. Check Events endpoint: `http://localhost:8000/docs#/Events`
4. Look at backend player state: should update scene_id

---

## 📖 Example: Complete Setup Code

Here's a simple example showing the full integration:

```csharp
using UnityEngine;
using VirtuLingo.BackendIntegration;

public class SimpleGameController : MonoBehaviour
{
    // Assigned in inspector
    public LocationZoneManager locationManager;
    public SimplePushToTalkController pushToTalk;
    public GameEventSyncManager gameEventSync;

    void Start()
    {
        // Subscribe to zone changes
        locationManager.OnZoneChanged += HandleZoneChanged;
        
        // Setup initial state
        Debug.Log("VirtuLingo initialized!");
    }

    void HandleZoneChanged(string zoneId, string displayName)
    {
        Debug.Log($"Player entered: {displayName}");
        
        // Zone automatically syncs to backend via LocationZoneManager
        // No need to manually call gameEventSync.OnSceneChanged()
    }

    // Example: Player picks up item
    void OnPickupItem(string itemId)
    {
        gameEventSync.OnPickedObject(itemId);
        Debug.Log($"Picked up: {itemId}");
    }

    // Example: Player completes quest
    void OnQuestComplete(string questId)
    {
        gameEventSync.OnQuestCompleted(questId);
        Debug.Log($"Quest completed: {questId}");
    }
}
```

---

## 🎯 Best Practices

### Zone Design

✅ **DO:**
- Make zones overlap slightly (prevents gaps)
- Use descriptive zone IDs ("bakery" not "zone_01")
- Keep zone count reasonable (3-10 zones)
- Match zone IDs to backend expectations
- Use Zone Trigger colliders for debugging (see size in editor)

❌ **DON'T:**
- Create too many tiny zones (performance)
- Use complex zone shapes (stick to boxes)
- Forget to enable "Is Trigger"
- Use spaces in zone IDs (use underscores: "town_square")

### Push-to-Talk UX

✅ **DO:**
- Show clear visual feedback when recording
- Indicate when backend is processing
- Show when NPC is speaking
- Give feedback on connection status

❌ **DON'T:**
- Record without user knowing
- Play audio while user is speaking
- Hide connection errors from user

### State Management

✅ **DO:**
- Send meaningful state changes (zone, quest, items)
- Keep state simple and focused
- Use events for significant actions
- Test with backend logs to verify sync

❌ **DON'T:**
- Send position updates every frame (use zones!)
- Send redundant data
- Forget to sync important state changes

---

## 🏗️ Architecture Overview

### Data Flow

```
Unity Scene
    │
    ├─ Player Movement
    │     └─→ LocationZoneManager → GameEventSyncManager → Backend (scene_id)
    │
    ├─ Push-to-Talk (F Key)
    │     └─→ SimplePushToTalkController
    │           └─→ AudioCaptureManager → ConversationWebSocketManager
    │                                         ↓
    │                                    Backend STT
    │                                         ↓
    │                                    LLM + Grammar Check
    │                                         ↓
    │                                    TTS Audio
    │                                         ↓
    │     AudioPlaybackManager ←─ ConversationWebSocketManager
    │
    └─ Grammar Feedback
          └─ GrammarFeedbackUI displays corrections
```

### Component Hierarchy

```
ConversationManager (GameObject)
├─ ConversationController
│  ├─ References: webSocketManager, audioCaptureManager, etc.
│  └─ Orchestrates conversation flow
├─ ConversationWebSocketManager
│  ├─ Config: BackendConfig
│  └─ Handles WebSocket connection
├─ AudioCaptureManager
│  ├─ Config: BackendConfig
│  └─ Records microphone
├─ AudioPlaybackManager
│  └─ Plays NPC voice
├─ AudioSource (Unity component)
│  └─ Used by AudioPlaybackManager
├─ GameEventSyncManager
│  ├─ Config: BackendConfig
│  └─ Syncs state to backend
├─ GrammarFeedbackUI
│  └─ Displays corrections
└─ ReviewSessionManager
   ├─ Config: BackendConfig
   └─ Manages learning reviews

Player (GameObject)
└─ LocationZoneManager
   └─ Game Event Sync: [GameEventSyncManager from ConversationManager]

PushToTalkController (GameObject)
└─ SimplePushToTalkController
   ├─ Conversation Controller: [ConversationController]
   └─ Game Event Sync: [GameEventSyncManager]

Scene Zones (GameObjects)
├─ Bakery_Zone (Box Collider, Is Trigger)
├─ Marketplace_Zone (Box Collider, Is Trigger)
└─ Cafe_Zone (Box Collider, Is Trigger)
```

---

## 🚀 Quick Reference

### Common Inspector Setup

**ConversationManager GameObject:**
```
ConversationController
├─ Web Socket Manager: [ConversationWebSocketManager on this]
├─ Audio Capture Manager: [AudioCaptureManager on this]
├─ Audio Playback Manager: [AudioPlaybackManager on this]
├─ Game Event Sync: [GameEventSyncManager on this]
└─ Grammar Feedback UI: [GrammarFeedbackUI on this]

ConversationWebSocketManager
└─ Config: VirtuLingoConfig (asset)

AudioCaptureManager
└─ Config: VirtuLingoConfig (asset)

GameEventSyncManager
└─ Config: VirtuLingoConfig (asset)

ReviewSessionManager
└─ Config: VirtuLingoConfig (asset)

GrammarFeedbackUI
├─ Feedback Panel: [UI Panel]
├─ Original Text: [TextMeshPro - shows your text]
├─ Correction Text: [TextMeshPro - shows corrected text]
├─ Explanation Text: [TextMeshPro - shows explanation]
├─ Category Text: [TextMeshPro - shows error type]
└─ Severity Indicator: [Image - optional color indicator]
```

**Player GameObject:**
```
LocationZoneManager
├─ Game Event Sync: [ConversationManager]
└─ Zones: [zone array, see Zone Setup]
```

**PushToTalkController GameObject:**
```
SimplePushToTalkController
├─ NPC ID: "baker_01"
├─ Conversation Controller: [ConversationManager]
├─ Game Event Sync: [ConversationManager]
└─ Auto Connect On Start: ✓
```

### Key Backend Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/conversations/ws/{player_id}/{npc_id}` | WebSocket | Voice chat |
| `/api/events/` | POST | Sync game state |
| `/api/review/check` | POST | Check for review session |

### Audio Specifications

| Property | Value | Notes |
|----------|-------|-------|
| Sample Rate | 16000 Hz | Don't change |
| Channels | 1 (Mono) | Microphone input |
| Format | PCM16 | 16-bit signed integer |
| Chunk Size | ~1600 samples | 0.1 second chunks |

---

## 📞 Need Help?

### Check These First:
1. Backend running? → `http://localhost:8000/docs`
2. Console errors? → Read the error message
3. Zone colliders? → "Is Trigger" enabled
4. Player tagged? → Tag: "Player"
5. Components wired? → Check inspector references

### Debug Mode:
Add this to any component to see detailed logs:
```csharp
void OnEnable() {
    Debug.Log($"[{GetType().Name}] Enabled");
}
```

---

🎮 **Ready to create immersive language learning conversations!** ✨

Start backend → Press Play → Hold F → Speak → Learn!
