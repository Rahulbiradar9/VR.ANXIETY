# Real-time Voice Interaction System

A production-ready FastAPI backend and Unity client that enables real-time conversational voice interaction using WebSocket streaming.

## Features
- **FastAPI Backend**: Handles incoming audio via both REST (`/process-audio`) and WebSocket (`/ws/audio-stream`).
- **Real-time Processing**: Transcribes audio, generates LLM responses, and converts text back to speech.
- **Session Context**: Keeps track of conversation history per `session_id`.
- **Unity C# Client**: A push-to-talk style script that uses WebSockets to stream microphone audio to the backend and plays the returning audio.

## Architecture & Tech Stack
- **API Framework**: FastAPI, Uvicorn, websockets
- **Audio Processing**: miniaudio
- **Speech-to-Text (STT)**: Google Web Speech API via `SpeechRecognition`
- **Language Model**: Groq Llama 3.1 8B API
- **Text-to-Speech (TTS)**: Microsoft Edge TTS via `edge-tts` (High-quality Neural Voices)

## Running Locally

### 1. Set up Environment Variables
Create a file named `.env` in the root directory (alongside `PythonServer` and `Anxity.2` folders):
```
GROQ_API_KEY=your_groq_api_key_here
```

### 2. Using Python Virtual Environment
We recommend running this via a standard Python virtual environment.
```bash
cd PythonServer

# Create virtual environment if you haven't already
python -m venv venv

# Activate it
# Windows:
venv\Scripts\activate
# Mac/Linux:
# source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Run the server
uvicorn main:app --reload
```

## Unity Client Setup
1. Open your Unity project.
2. Ensure you have the [NativeWebSocket package](https://github.com/endel/NativeWebSocket) installed in your Unity project.
3. Attach the `VoiceInteractionClient.cs` script to any active GameObject in your Scene.
4. Assign an `AudioSource` component to the script in the Inspector.
5. Hit Play in Unity. The client will automatically start processing audio and sending it to the backend. The backend will process the speech and the Unity client will automatically play the synthesized voice response.

## REST Endpoint Testing
If you wish to test the fallback REST endpoint quickly with `curl`:
```bash
curl -X POST "http://localhost:8000/process-audio" \\
  -H "accept: application/json" \\
  -H "Content-Type: multipart/form-data" \\
  -F "file=@test_audio.wav" \\
  -F "session_id=123"
```
