from fastapi import FastAPI, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
import shutil
from speech import speech_to_text
from ai import generate_question, reset_conversation

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

import os
import base64
import pyttsx3

@app.post("/upload-audio")
async def upload_audio(file: UploadFile = File(...)):

    file_location = "audio.wav"

    with open(file_location, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    text = speech_to_text(file_location)

    question = generate_question(text)

    # Generate TTS audio
    output_audio_path = "response.wav"
    engine = pyttsx3.init()
    engine.save_to_file(question, output_audio_path)
    engine.runAndWait()

    # Read the audio and encode to base64
    if os.path.exists(output_audio_path):
        with open(output_audio_path, "rb") as audio_file:
            audio_bytes = audio_file.read()
            audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
    else:
        audio_base64 = ""

    return {
        "user_text": text,
        "ai_question": question,
        "audio_base64": audio_base64
    }

@app.get("/get-greeting")
async def get_greeting():
    greeting_text = "Tell me your name, which topic are we willing to talk about?"
    
    # Generate TTS audio
    output_audio_path = "greeting.wav"
    engine = pyttsx3.init()
    engine.save_to_file(greeting_text, output_audio_path)
    engine.runAndWait()

    # Read the audio and encode to base64
    if os.path.exists(output_audio_path):
        with open(output_audio_path, "rb") as audio_file:
            audio_bytes = audio_file.read()
            audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
    else:
        audio_base64 = ""

    return {
        "ai_question": greeting_text,
        "audio_base64": audio_base64
    }

@app.post("/reset-conversation")
async def reset_conv():
    reset_conversation()
    return {"status": "Conversation reset."}