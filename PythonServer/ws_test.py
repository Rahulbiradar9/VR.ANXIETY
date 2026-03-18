import asyncio
import websockets
import json

async def test_websocket():
    url = "ws://localhost:8000/ws/audio-stream"
    try:
        print(f"Connecting to {url}...")
        async with websockets.connect(url) as websocket:
            print("Connected successfully!")
            
            # Send initial metadata
            metadata = json.dumps({"session_id": "python-test-123"})
            await websocket.send(metadata)
            print("Sent metadata")
            
            # Try to receive a response
            try:
                # Wait for up to 3 seconds for a response
                response = await asyncio.wait_for(websocket.recv(), timeout=3.0)
                print(f"Received from server: {response}")
            except asyncio.TimeoutError:
                print("No immediate response from server (expected if waiting for audio)")
                
    except Exception as e:
        print(f"Connection failed: {e}")

if __name__ == "__main__":
    asyncio.run(test_websocket())
