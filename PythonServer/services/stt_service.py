import os
import speech_recognition as sr
import tempfile
import aiofiles
import asyncio

class STTService:
    def __init__(self):
        self.recognizer = sr.Recognizer()

    async def transcribe_audio(self, audio_data: bytes) -> str:
        """
        Transcribes the bytes of an audio file using Google Web Speech API (Free).
        """
        # Write to temporary file
        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as temp_audio:
            temp_path = temp_audio.name
        
        try:
            async with aiofiles.open(temp_path, "wb") as f:
                await f.write(audio_data)

            def _sync_transcribe():
                with sr.AudioFile(temp_path) as source:
                    audio = self.recognizer.record(source)
                try:
                    # Specifying the language explicitly speeds up TTFB tremendously
                    return self.recognizer.recognize_google(audio, language="en-US")
                except sr.UnknownValueError:
                    return ""
                except sr.RequestError as e:
                    print(f"Could not request results from Google Speech Recognition service; {e}")
                    return ""

            transcript = await asyncio.to_thread(_sync_transcribe)
            return transcript.strip()
            
        except Exception as e:
            print(f"STT Error: {e}")
            return ""
        finally:
            if os.path.exists(temp_path):
                os.remove(temp_path)
