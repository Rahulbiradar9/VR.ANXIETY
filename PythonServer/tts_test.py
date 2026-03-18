import asyncio
from services.tts_service import TTSService

async def test_tts():
    tts = TTSService()
    print("Generating audio...")
    audio_bytes = await tts.generate_audio("Hello, testing the PCM output.")
    print(f"Generated {len(audio_bytes)} bytes of raw PCM audio.")
    
if __name__ == "__main__":
    asyncio.run(test_tts())
