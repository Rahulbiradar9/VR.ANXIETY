import os
from gtts import gTTS
import tempfile
import asyncio

class TTSService:
    async def generate_audio(self, text: str) -> bytes:
        """
        Generates TTS audio from text using gTTS.
        Returns the raw audio bytes (MP3 format from gTTS).
        In production, Coqui TTS or OpenAI TTS might be used for lower latency or higher quality.
        """
        # Run synchronous gTTS and pydub in a thread
        # Run synchronous gTTS and miniaudio in a thread
        def _sync_gtts_to_pcm():
            tts = gTTS(text=text, lang='en', slow=False)
            
            # Save MP3
            with tempfile.NamedTemporaryFile(delete=False, suffix=".mp3") as temp_mp3:
                mp3_path = temp_mp3.name
            tts.save(mp3_path)
            
            # Use miniaudio to decode MP3 directly to raw PCM without ffmpeg
            import miniaudio
            
            # Decode file
            decoded = miniaudio.decode_file(mp3_path, nchannels=1, sample_rate=16000)
            pcm_bytes = decoded.samples.tobytes()
                
            os.remove(mp3_path)
            return pcm_bytes
            
        try:
            pcm_bytes = await asyncio.to_thread(_sync_gtts_to_pcm)
            return pcm_bytes
        except Exception as e:
            print(f"TTS Error: {e}")
            return b""
