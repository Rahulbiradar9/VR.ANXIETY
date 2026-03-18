from fastapi import FastAPI, UploadFile, File, Form, WebSocket, WebSocketDisconnect
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import base64
import uuid

# Import services
from services.vad_service import VADService
from services.stt_service import STTService
from services.llm_service import LLMService
from services.tts_service import TTSService
from services.session_manager import SessionManager

app = FastAPI(title="Voice Interaction Backend")

# Middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Services Init
vad_service = VADService()
stt_service = STTService()
llm_service = LLMService()
tts_service = TTSService()
session_manager = SessionManager(session_timeout_minutes=30)

@app.get("/")
async def root():
    """Health check endpoint"""
    return {"message": "Voice Interaction API is running."}

@app.post("/process-audio")
async def process_audio(
    file: UploadFile = File(...), 
    session_id: str = Form(...)
):
    """
    REST endpoint fallback. 
    Receives an audio file (e.g. .wav) and a session_id.
    Returns text response and base64 encoded audio response.
    """
    try:
        audio_bytes = await file.read()
        
        # 1. Speech to Text
        user_text = await stt_service.transcribe_audio(audio_bytes)
        print(f"[{session_id}] User says: {user_text}")
        
        if not user_text:
             return JSONResponse(status_code=400, content={"error": "Could not understand audio."})

        # 2. Add to Session History & Get LLM Response
        session_manager.add_message(session_id, "user", user_text)
        history = session_manager.get_context(session_id)
        
        llm_response = await llm_service.generate_response(history)
        print(f"[{session_id}] Bot says: {llm_response}")
        session_manager.add_message(session_id, "assistant", llm_response)

        # 3. Text to Speech
        tts_audio_bytes = await tts_service.generate_audio(llm_response)
        b64_audio = base64.b64encode(tts_audio_bytes).decode('utf-8')

        return {
            "session_id": session_id,
            "text": llm_response,
            "audio_base64": b64_audio
        }

    except Exception as e:
        print(f"Error in /process-audio: {e}")
        return JSONResponse(status_code=500, content={"error": str(e)})

@app.websocket("/ws/audio-stream")
async def websocket_endpoint(websocket: WebSocket):
    """
    WebSocket endpoint for real-time continuous voice interaction.
    Client should connect and send the initial message with the session_id: {"session_id": "xyz"}
    Then send audio chunks as binary.
    """
    await websocket.accept()
    session_id = str(uuid.uuid4()) # Default random
    
    # Receive metadata first
    try:
        data = await websocket.receive_json()
        if 'session_id' in data:
            session_id = data['session_id']
            print(f"WebSocket connected for session: {session_id}")
    except Exception as e:
        print(f"WebSocket metadata parse error using random session_id={session_id}")

    try:
        while True:
            # Receive audio frame (binary)
            audio_data = await websocket.receive_bytes()
            
            # Simple VAD logic could be inserted here. For a truly continuous stream,
            # you'd buffer chunks until VAD detects silence. Then process the buffer.
            # To emulate simplicity without complex buffering, let's treat the incoming
            # bytes as a complete utterance for now (client controls chunk).
            
            user_text = await stt_service.transcribe_audio(audio_data)
            print(f"[{session_id}] WS User says: {user_text}")

            if not user_text:
                await websocket.send_json({"type": "status", "message": "Listening..."})
                continue
                
            session_manager.add_message(session_id, "user", user_text)
            history = session_manager.get_context(session_id)
            
            llm_response = await llm_service.generate_response(history)
            print(f"[{session_id}] WS Bot says: {llm_response}")
            session_manager.add_message(session_id, "assistant", llm_response)
            
            # Send text response immediately
            await websocket.send_json({
                "type": "text",
                "text": llm_response
            })
            
            # Generate TTS and send binary audio to play
            tts_audio_bytes = await tts_service.generate_audio(llm_response)
            
            # Send binary frame to client
            # Protocol: Send JSON to indicate audio is coming, then send Bytes
            await websocket.send_json({"type": "audio_start"})
            await websocket.send_bytes(tts_audio_bytes)
            await websocket.send_json({"type": "audio_end"})

    except WebSocketDisconnect:
        print(f"WebSocket disconnected for session: {session_id}")
    except Exception as e:
        print(f"WebSocket Error: {e}")
        try:
           await websocket.close()
        except:
            pass

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
