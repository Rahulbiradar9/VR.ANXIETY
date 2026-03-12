# WORK IN PROGRESS
## VR.ANXIETY

VR.ANXIETY is a virtual reality application combining Unity 3D with an intelligent Python AI Backend. It simulates conversational interactions for users—such as an interview training session—using advanced local AI modeling to evaluate and engage with the user via continuous voice-to-voice interaction. 

The project aims to provide a safe, AI-driven environment where users can practice communication and tackle social anxiety by talking to a responsive virtual character.

## 🚀 Key Features
- **Continuous Voice Interaction**: Seamless microphone recording and silence detection directly in Unity.
- **FastAPI Python Backend**: High-performance HTTP server handling the audio processing logic.
- **Speech-to-Text (STT)**: Integration with OpenAI Whisper for accurate speech transcripts.
- **Local Large Language Model (LLM)**: Powered by `Ollama` running the `Llama 3` model locally for instant, private conversation generation.
- **Text-to-Speech (TTS)**: Employs `pyttsx3` to convert the AI's textual response back to an audio stream for the Unity client.

---

## 🛠️ Technology Stack
### Frontend (Unity Environment)
- **Engine**: Unity 3D
- **Language**: C#
- **Capabilities**: Microphone API, UnityWebRequest for asynchronous communication.

### Backend (Python Server)
- **Framework**: `FastAPI` (with `uvicorn` server)
- **STT**: `openai-whisper`
- **TTS**: `pyttsx3`
- **Generative AI**: `Ollama` (Llama 3 Model)
- **Audio Processing**: `python-multipart`, Base64 encoding for payload deliveries.

---

## ⚙️ Prerequisites
Before running the project, make sure you have the following installed to ensure proper setup:
1. **Python 3.8+** (for the backend server).
2. **Unity Hub** & **Unity Editor** (Compatible with standard modern 3D pipelines).
3. **Ollama**: Local AI runner. You can download and install it from [ollama.ai](https://ollama.ai/).

---

## 📥 Installation & Setup

### 1. Backend Server Setup (Python)
Navigate to the `PythonServer` directory:
```bash
cd PythonServer
```

Create a virtual environment (optional but recommended):
```bash
python -m venv venv

# On Windows:
venv\Scripts\activate
# On MacOS/Linux:
source venv/bin/activate
```

Install the required package dependencies using the `requirements.txt` file:
```bash
pip install -r requirements.txt
```

This will install all necessary packages with their corresponding versions, including:
- `fastapi==0.115.*`
- `uvicorn==0.34.*`
- `python-multipart==0.0.*`
- `requests==2.32.*`
- `openai-whisper==20240930`
- `pyttsx3==2.90`

*(Note: Depending on your system, you might still need to install `torch`, `torchvision`, and `torchaudio` manually for optimal performance.)*

**Note on Whisper & PyTorch**: You may need to separately install PyTorch according to your system specs (CUDA vs CPU) [from PyTorch's official site](https://pytorch.org/) to run Whisper effectively. Furthermore, `pyttsx3` relies on system TTS drivers (e.g., SAPI5 on Windows). Ensure these are enabled on your OS.

### 2. Ollama & Llama 3 Setup
Make sure the Ollama application is running in the background. Then pull the Llama 3 model by running:
```bash
ollama run llama3
```
Keep the Ollama service running on the default port `localhost:11434`.

### 3. Unity Client Setup (C#)
1. Open the **Unity Hub** and select the `UnityENV/Anxity` project folder to open the project.
2. In the Unity Editor, verify that the `MicrophoneRecorder` and `SendAudioAPI` scripts are attached to the relevant GameObject in your Scene.
3. Keep the server URL defined in `SendAudioAPI` pointed to your local Python server (`http://localhost:8000`).

---

## 🎯 Usage Instructions

1. **Start the Python Backend**:
   Navigate to the `PythonServer` folder, activate the virtual environment if applicable, and launch the FastAPI server:
   ```bash
   uvicorn main:app --host 0.0.0.0 --port 8000 --reload
   ```

2. **Run Ollama**:
   Ensure `ollama run llama3` or the Ollama service is active.

3. **Play in Unity**:
   - Hit **Play** in the Unity Editor or start your built application. 
   - Wait for the initial AI greeting ("Tell me your name...").
   - Begin your conversation! The interaction continuously listens for your voice, detects when you stop speaking, and seamlessly routes it through the backend to formulate an AI auditory response.

## 📄 License
See the `LICENSE` file for details.
