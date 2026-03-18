import os
from dotenv import load_dotenv

# Load environment variables (check both PythonServer dir and project root)
env_path = os.path.join(os.path.dirname(__file__), '..', '.env')
load_dotenv(dotenv_path=env_path)
load_dotenv() # Fallback to local .env if it exists
GROQ_API_KEY = os.getenv("GROQ_API_KEY")
if not GROQ_API_KEY:
    print("WARNING: GROQ_API_KEY is not set in environment or .env file. Please get a free key from console.groq.com")

# Global settings
SAMPLE_RATE = 16000 # Whisper expects 16kHz audio
CHUNK_DURATION_MS = 30 # For VAD

