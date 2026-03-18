import tempfile
import asyncio

class TTSService:
    async def generate_audio(self, text: str) -> bytes:
        """
        Generates TTS audio from text using edge-tts (female voice).
        Returns the raw audio bytes (MP3 format).
        """
        import edge_tts
        
        # Use a high-quality female English voice
        voice = "en-US-AriaNeural" 
        
        try:
            communicate = edge_tts.Communicate(text, voice)
            audio_bytes = b""
            async for chunk in communicate.stream():
                if chunk["type"] == "audio":
                    audio_bytes += chunk["data"]
                    
            # Use miniaudio to decode MP3 directly to raw PCM without ffmpeg
            import miniaudio
            
            # Since miniaudio works with files/memory buffers, let's process the bytes
            decoded = miniaudio.decode(audio_bytes, nchannels=1, sample_rate=16000)
            pcm_bytes = decoded.samples.tobytes()
            
            return pcm_bytes
            
        except Exception as e:
            print(f"TTS Error: {e}")
            return b""
