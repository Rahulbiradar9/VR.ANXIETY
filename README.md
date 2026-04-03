# VR Anxiety Therapy — AI Voice Interaction System 

A full-stack VR therapy application combining a **FastAPI AI backend** with a **Unity VR client** (Meta Quest / OpenXR). A 3D character (Alice) holds real-time spoken conversations with the user using voice activity detection, speech recognition, an LLM, neural text-to-speech, and a live **dialog canvas UI** that shows what Alice is saying.

---

## Architecture Overview

```
User (VR Headset / Desktop)
     │  (speaks)
     ▼
Unity VR Client  (Anxity.2)
  ├── Microphone capture + client-side VAD (RMS silence detection)
  ├── Sends WAV audio via WebSocket  (/ws/audio-stream)
  ├── Receives  {"type":"text",  "text":"..."}  → displays on TextMeshPro Canvas
  ├── Receives  {"type":"audio_start"}  then raw PCM bytes then {"type":"audio_end"}
  ├── Decodes PCM → AudioClip → plays through AudioSource
  ├── Triggers isTalking animator parameter (lip-sync)
  └── World-Space UI Canvas displays text beside character
     │
     ▼
FastAPI Backend  (PythonServer)
  ├── STT : Google Web Speech API  (SpeechRecognition)
  ├── LLM : Groq — Llama 3.1 8B Instant
  └── TTS : Microsoft Edge TTS (edge-tts) → raw 16-bit PCM via miniaudio
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| API Framework | FastAPI + Uvicorn |
| Speech-to-Text | Google Web Speech API (`SpeechRecognition`) |
| Language Model | Groq Cloud — `llama-3.1-8b-instant` |
| Text-to-Speech | Microsoft Edge TTS (`edge-tts`) + `miniaudio` for PCM decode |
| Voice Activity Detection | RMS silence detection (Unity client-side) |
| Session Memory | In-memory session manager with 30-min timeout |
| Unity Version | Unity 6 (or 2022 LTS) |
| VR SDK | XR Interaction Toolkit + Oculus / OpenXR |
| Transport | WebSocket (`/ws/audio-stream`) + REST fallback (`/process-audio`) |
| Dialog UI | World-Space Canvas with TextMeshPro — attached to character |

---

## Project Structure

```
2.0_Trial/
├── .env                              # API keys (NOT committed — add your own)
├── .gitignore
├── README.md
│
├── PythonServer/                     # FastAPI backend
│   ├── main.py                       # App entry: REST + WebSocket endpoints
│   ├── config.py                     # Loads env vars & global settings
│   ├── requirements.txt              # Python dependencies
│   └── services/
│       ├── stt_service.py            # Speech-to-Text (Google)
│       ├── llm_service.py            # LLM responses (Groq / Llama 3.1)
│       ├── tts_service.py            # Text-to-Speech (Edge TTS → PCM)
│       ├── vad_service.py            # Voice Activity Detection
│       └── session_manager.py        # Conversation history per session
│
└── Anxity.2/                         # Unity VR project (Unity 6 / 2022 LTS)
    └── Assets/
        ├── Scripts/
        │   ├── VoiceInteractionClient.cs    # Primary WebSocket client + VAD
        │   ├── AICharacterController.cs     # HTTP fallback client + animator
        │   └── UnityMainThreadDispatcher.cs # Thread-safe Unity API calls
        ├── MainAnimatorController.controller
        ├── Scenes/                           # Unity scenes
        └── XRI/                              # XR Interaction Toolkit config
```

---

## Quick Start

### Prerequisites

- Python 3.10+
- [Groq API Key](https://console.groq.com) (free tier available)
- Unity 2022 LTS or Unity 6
- XR Interaction Toolkit installed via Unity Package Manager
- Meta Quest headset **or** Unity Play Mode (XR Device Simulator)

---

### 1. Configure Environment Variables

Create a `.env` file in the root `2.0_Trial/` folder:

```env
GROQ_API_KEY=your_groq_api_key_here
```

---

### 2. Run the Python Backend

```bash
cd PythonServer

# Create virtual environment (first time only)
python -m venv venv

# Activate — Windows
venv\Scripts\activate
# Activate — macOS / Linux
# source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Start server
uvicorn main:app --host 0.0.0.0 --port 8000
```

> Server runs at `http://localhost:8000`  
> Swagger docs at `http://localhost:8000/docs`

---

### 3. Unity VR Client Setup

1. Open the `Anxity.2` folder as a Unity project.
2. Install packages via **Window → Package Manager**:
   - **XR Interaction Toolkit**
   - **NativeWebSocket** (from [GitHub](https://github.com/endel/NativeWebSocket))
   - **Oculus XR Plugin** (for Meta Quest) or **OpenXR Plugin**
3. Open your scene under `Assets/Scenes/`.
4. Select your 3D character and:
   - Add **AudioSource** → set **Spatial Blend** to `1` (3D)
   - Add **Animator** → assign `MainAnimatorController`
   - Add **Voice Interaction Client** script
5. In the **Voice Interaction Client** Inspector:
   - Set `Websocket Url` to `ws://<YOUR_PC_LAN_IP>:8000/ws/audio-stream`
   - *(Find your IP via `ipconfig` on Windows)*
6. **UI Canvas Setup:**
   - Create a **Canvas** under your character (`GameObject > UI > Canvas`)
   - Change Canvas **Render Mode** to `World Space`
   - Scale down Canvas (e.g., `0.005`) and position it above the character
   - Add a **Text - TextMeshPro** element to the Canvas
   - Drag the TextMeshPro element into the `Response Text Display` slot of the **Voice Interaction Client** script
7. **Animator Controller** (`MainAnimatorController`):
   - Add a **Bool** parameter named `isTalking`
   - Transition `Idle → Talking` when `isTalking = true` (no exit time)
   - Transition `Talking → Idle` when `isTalking = false` (no exit time)

---

### 4. Build for Meta Quest

1. **File → Build Settings → Android** → Switch Platform
2. **Player Settings → Android → Other Settings:**
   - Minimum API Level: `Android 10 (API 29)`
3. **XR Plug-in Management → Android:** Enable **Oculus** or **OpenXR**
4. Connect Quest via USB, enable Developer Mode
5. **Build and Run**

> ⚠️ Quest and your PC must be on the **same WiFi network**

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/` | Health check |
| `POST` | `/process-audio` | WAV file → JSON `{text, audio_base64}` |
| `WS` | `/ws/audio-stream` | Real-time WebSocket — greeting on connect, then bidirectional |

### WebSocket Protocol

**Server → Client messages:**

| Message | Meaning |
|---|---|
| `{"type":"text","text":"..."}` | Alice's reply text (shown on Dialog Canvas) |
| `{"type":"audio_start"}` | Raw PCM bytes follow |
| `{"type":"audio_end"}` | PCM stream complete, play the clip |
| `{"type":"status","message":"Listening..."}` | User audio was silent/empty |

**Client → Server:**
- First message: `{"session_id": "<uuid>"}` (JSON)
- Subsequent: raw WAV bytes (binary)

---

## How It Works (Conversation Flow)

1. Unity connects WebSocket → server sends greeting text + audio
2. TextMeshPro Canvas displays greeting text beside Alice
3. Continuous microphone loop detects speech via RMS VAD
4. After 1.5 s silence → WAV chunk sent to server
5. Server: STT → LLM → TTS → sends `text` message then PCM audio
6. Unity shows AI text on the world-space UI canvas immediately
7. Unity decodes PCM → plays through `AudioSource`, sets `isTalking = true`
8. Animation plays. When audio ends, `isTalking = false` → resuming listening

---

## Troubleshooting

| Problem | Fix |
|---|---|
| UI text doesn't update | Make sure your Text (TMP) object is assigned to the **Response Text Display** slot in the Inspector. |
| Text appears giant/clipping | Ensure the Canvas is set to **World Space** and Scale is adjusted to a small value like `0.005`. |
| WebSocket won't connect | Check that the backend is running and the URL uses your PC's LAN IP, not `localhost` |
| No audio playback | Verify `AudioSource` is assigned on the character. Check Unity console for PCM decode errors. |
| "isTalking" has no effect | Add the Bool parameter in the Animator Controller and set up the transitions. |
| Quest can't reach server | Both devices must be on the same WiFi. Disable Windows Firewall for port 8000 if needed. |
