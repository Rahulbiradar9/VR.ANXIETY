# VR Anxiety Therapy — AI Voice Interaction System

A full-stack VR therapy application combining a **FastAPI AI backend** with a **Unity VR client** (Meta Quest / OpenXR). A 3D doctor/interviewer character holds real-time spoken conversations with the user using voice activity detection, speech recognition, an LLM, and neural text-to-speech.

---

## Architecture Overview

```
User (VR Headset)
     │  (speaks)
     ▼
Unity VR Client (Anxity.2)
  ├── Microphone capture + VAD (silence detection)
  ├── Sends WAV audio via HTTP POST
  └── Receives JSON → plays PCM audio + triggers Talking animation
     │
     ▼
FastAPI Backend (PythonServer)
  ├── STT:  Google Web Speech API  (SpeechRecognition)
  ├── LLM:  Groq — Llama 3.1 8B Instant
  └── TTS:  Microsoft Edge TTS (edge-tts) → raw 16-bit PCM via miniaudio
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| API Framework | FastAPI + Uvicorn |
| Speech-to-Text | Google Web Speech API (`SpeechRecognition`) |
| Language Model | Groq Cloud — `llama-3.1-8b-instant` |
| Text-to-Speech | Microsoft Edge TTS (`edge-tts`) + `miniaudio` for PCM decode |
| Voice Activity Detection | `webrtcvad` (server-side) + RMS silence detection (client-side) |
| Session Memory | In-memory session manager with 30-min timeout |
| Unity Version | Unity 6 (or 2022 LTS) |
| VR SDK | XR Interaction Toolkit + Oculus / OpenXR |
| Transport | REST (`/process-audio`) + WebSocket (`/ws/audio-stream`) |

---

## Project Structure

```
2.0_Trial/
├── .env                          # API keys (not committed)
├── .gitignore
├── README.md
│
├── PythonServer/                 # FastAPI backend
│   ├── main.py                   # App entry point, REST + WebSocket endpoints
│   ├── config.py                 # Loads env vars & global settings
│   ├── requirements.txt          # Python dependencies
│   ├── ws_test.py                # WebSocket manual test client
│   ├── tts_test.py               # TTS standalone test
│   └── services/
│       ├── stt_service.py        # Speech-to-Text (Google)
│       ├── llm_service.py        # LLM responses (Groq / Llama 3.1)
│       ├── tts_service.py        # Text-to-Speech (Edge TTS → PCM)
│       ├── vad_service.py        # Voice Activity Detection (webrtcvad)
│       └── session_manager.py   # Conversation history per session
│
└── Anxity.2/                     # Unity VR project
    └── Assets/
        ├── Scripts/
        │   ├── AICharacterController.cs   # Main VR integration script
        │   └── VoiceInteractionClient.cs  # (Legacy) WebSocket client
        ├── MainAnimatorController.controller
        ├── Scenes/               # Unity scenes
        └── XRI/                  # XR Interaction Toolkit config
```

---

## Quick Start

### Prerequisites

- Python 3.10+
- [Groq API Key](https://console.groq.com) (free)
- Unity 2022 LTS or Unity 6
- XR Interaction Toolkit installed in Unity
- Meta Quest headset OR Unity Play Mode (XR Simulator)

---

### 1. Configure Environment Variables

Create a `.env` file in the **root** `2.0_Trial/` folder:

```env
GROQ_API_KEY=your_groq_api_key_here
```

---

### 2. Run the Python Backend

```bash
cd PythonServer

# Create and activate virtual environment (first time only)
python -m venv venv

# Windows
venv\Scripts\activate
# macOS / Linux
# source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Start the server
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

> The server will be available at `http://localhost:8000`  
> API docs at `http://localhost:8000/docs`

---

### 3. Unity VR Client Setup

1. Open the `Anxity.2` folder as a Unity project.
2. Install packages via **Window → Package Manager**:
   - **XR Interaction Toolkit**
   - **Oculus XR Plugin** (for Meta Quest) or **OpenXR Plugin**
3. Open your Scene under `Assets/Scenes/`.
4. Select your 3D doctor character and:
   - Add **AudioSource** component → set **Spatial Blend** to `1` (3D)
   - Add **Animator** component → assign `MainAnimatorController`
   - Add **AI Character Controller** script → drag in Animator & AudioSource
5. In the **AI Character Controller** Inspector:
   - Set `Api Endpoint` to your PC's LAN IP: `http://192.168.X.X:8000/process-audio`
   - *(Find your IP by running `ipconfig` in terminal)*
6. Configure the **Animator Controller** (`MainAnimatorController`):
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
| `POST` | `/process-audio` | Send WAV file, receive JSON with text + base64 PCM audio |
| `WS` | `/ws/audio-stream` | Real-time WebSocket streaming (sends greeting on connect) |

### POST `/process-audio` — Request

```
Content-Type: multipart/form-data
Fields:
  file        (WAV audio file)
  session_id  (string, UUID)
```

### POST `/process-audio` — Response

```json
{
  "session_id": "abc-123",
  "text": "That sounds really stressful. Can you tell me more?",
  "audio_base64": "<base64-encoded raw 16-bit PCM @ 16kHz mono>"
}
```

---

## Testing Without a Headset

Test the REST endpoint with curl:
```bash
curl -X POST "http://localhost:8000/process-audio" \
  -F "file=@test_audio.wav" \
  -F "session_id=test-session-1"
```

Run the included WebSocket test client:
```bash
cd PythonServer
python ws_test.py
```

---

## How It Works (Conversation Flow)

1. Unity starts microphone recording in a **continuous loop**
2. **Client-side VAD**: RMS energy is measured every 256 samples
3. When speech is detected → audio is buffered
4. After 1.5 seconds of silence → WAV is sent to `/process-audio`
5. AI state switches to `Processing` (mic is paused to prevent feedback)
6. Backend returns response text + base64 PCM audio
7. Unity decodes PCM → creates `AudioClip` → plays through `AudioSource`
8. Animator sets `isTalking = true` during playback, `false` when done
9. State returns to `Listening` → loop continues
