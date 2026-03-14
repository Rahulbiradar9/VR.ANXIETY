# WORK IN PROGRESS
## VR.ANXIETY

VR.ANXIETY is a virtual reality application combining Unity 3D with an intelligent Python AI Backend. It simulates conversational interactions for users—such as an interview training session—using advanced local AI modeling to engage with the user via continuous voice-to-voice interaction.

The project aims to provide a safe, AI-driven environment where users can practice communication and tackle social anxiety by talking to a responsive virtual character.

---

## 🚀 Key Features

- **Continuous Voice Interaction**: Seamless microphone recording and silence detection directly in Unity via `MicrophoneRecorder.cs`.
- **WAV Audio Pipeline**: Audio is recorded in WAV format in Unity, sent to the server, and WAV responses are returned and played back.
- **FastAPI Python Backend**: High-performance HTTP server (`main.py`) handling all audio processing logic.
- **Speech-to-Text (STT)**: Powered by `faster-whisper` (base model, CPU, int8) for fast and accurate local speech transcription.
- **Local Large Language Model (LLM)**: Powered by `Ollama` running the `Llama 3` model locally for instant, private conversation generation.
- **Text-to-Speech (TTS)**: Uses `pyttsx3` to convert the AI's text response back to a WAV audio stream for the Unity client.
- **Base64 Audio Transport**: Audio is base64-encoded in the JSON response, decoded in Unity, written to disk, and played through Unity's `AudioSource`.

---

## 🗂️ Project Structure

```
trial/
├── PythonServer/               # Python FastAPI backend
│   ├── main.py                 # FastAPI app — /upload-audio, /get-greeting, /reset-conversation
│   ├── ai.py                   # Ollama (Llama 3) conversation logic
│   ├── speech.py               # faster-whisper speech-to-text
│   ├── requirements.txt        # Python dependencies
│   ├── test.py                 # Quick STT test script
│   └── venv/                   # Python virtual environment
│
└── UnityENV/Anxity/            # Unity 3D project
    └── Assets/Scripts/
        ├── MicrophoneRecorder.cs   # Records mic input, detects silence, saves recorded.wav
        ├── SendAudioAPI.cs         # Sends WAV to backend, receives and plays audio response
        └── WavUtility.cs           # Helper for WAV encoding/decoding
```

---

## 🛠️ Technology Stack

### Frontend — Unity Environment
| Component | Detail |
|---|---|
| Engine | Unity 3D |
| Language | C# |
| Audio Recording | Unity Microphone API → WAV via `WavUtility.cs` |
| Networking | `UnityWebRequest` (multipart form upload + JSON response) |

### Backend — Python Server
| Component | Library / Tool |
|---|---|
| Framework | `FastAPI` + `uvicorn` |
| Speech-to-Text | `faster-whisper` (base model, CPU, int8) |
| Text-to-Speech | `pyttsx3` (SAPI5 on Windows) |
| Language Model | `Ollama` — `llama3` (local, no internet required) |
| Audio Transport | Base64-encoded WAV over JSON |

---

## ⚙️ Prerequisites

1. **Python 3.10+**
2. **Unity Hub** & **Unity Editor**
3. **Ollama** — download from [ollama.ai](https://ollama.ai/) and pull the model:
   ```bash
   ollama run llama3
   ```

---

## 📥 Installation & Setup

### 1. Backend — Python Server

```bash
cd PythonServer

# Create and activate virtual environment
python -m venv venv
venv\Scripts\activate        # Windows
# source venv/bin/activate   # macOS/Linux

# Install dependencies
pip install -r requirements.txt
```

**Packages installed:**

| Package | Purpose |
|---|---|
| `fastapi==0.115.*` | Web framework |
| `uvicorn==0.34.*` | ASGI server |
| `python-multipart==0.0.*` | File upload support |
| `requests==2.32.*` | HTTP calls to Ollama |
| `faster-whisper` | Speech-to-text (replaces openai-whisper) |
| `pyttsx3==2.90` | Text-to-speech (WAV output) |
| `edge-tts` | Alternative TTS (Microsoft Edge voices) |

> **Note:** `faster-whisper` downloads the `base` model (~145 MB) on first run. No Rust or C++ build tools are required (unlike `openai-whisper`).

> **Note:** `pyttsx3` uses SAPI5 on Windows. Ensure Windows TTS voices are installed (they are by default).

### 2. Ollama & Llama 3

```bash
# Pull and start the model (keep this running in a separate terminal)
ollama run llama3
```

Ollama must be running on `localhost:11434` before starting the Python server.

### 3. Unity Client

1. Open **Unity Hub** and load `UnityENV/Anxity`.
2. Ensure `MicrophoneRecorder` and `SendAudioAPI` scripts are attached to their GameObjects in the scene.
3. `SendAudioAPI` connects to `http://127.0.0.1:8000` by default — change this if running the server remotely.

---

## 🎯 Usage

**Step 1** — Start Ollama (if not already running):
```bash
ollama run llama3
```

**Step 2** — Start the Python backend:
```bash
cd PythonServer
venv\Scripts\activate
uvicorn main:app --reload
```

**Step 3** — Press **Play** in Unity:
- The app fetches an initial AI greeting and plays it back.
- Speak into your microphone — the app records, transcribes, gets an AI response, and plays the reply as audio automatically.
- Use the **Reset** button to start a new conversation.

---

## 🔌 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/upload-audio` | Accepts WAV file, returns `{user_text, ai_question, audio_base64}` |
| `GET` | `/get-greeting` | Returns initial greeting with audio |
| `POST` | `/reset-conversation` | Clears conversation history |

---

## 📄 License

See the `LICENSE` file for details.
