from faster_whisper import WhisperModel

model = WhisperModel("base", device="cpu", compute_type="int8")

def speech_to_text(audio_path):
    segments, _ = model.transcribe(audio_path)
    return " ".join(segment.text.strip() for segment in segments)