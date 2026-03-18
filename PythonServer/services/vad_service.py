import webrtcvad

class VADService:
    def __init__(self, mode=1):
        """
        mode: 0, 1, 2, or 3. 3 is the most aggressive in filtering out non-speech.
        """
        self.vad = webrtcvad.Vad(mode)

    def is_speech(self, audio_frame: bytes, sample_rate: int = 16000) -> bool:
        """
        Check if the audio frame contains speech.
        Frame duration must be 10, 20, or 30 ms.
        For 16kHz, 30ms = 480 samples = 960 bytes (16-bit PCM).
        """
        try:
            return self.vad.is_speech(audio_frame, sample_rate)
        except Exception as e:
            # If frame size is wrong, just assume speech to pass it along
            print(f"VAD Error: {e}")
            return True
