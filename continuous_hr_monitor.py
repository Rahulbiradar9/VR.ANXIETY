import asyncio
import sys
from bleak import BleakClient, BleakScanner

# Standard Bluetooth Low Energy UUIDs for Heart Rate
HEART_RATE_SERVICE_UUID = "0000180d-0000-1000-8000-00805f9b34fb"
HEART_RATE_MEASUREMENT_UUID = "00002a37-0000-1000-8000-00805f9b34fb"

def hr_data_handler(sender, data):
    flags = data[0]
    is_16_bit_hr = flags & 0x01
    
    if is_16_bit_hr:
        hr_value = int.from_bytes(data[1:3], byteorder="little")
    else:
        hr_value = data[1]
        
    print(f"💓 Heart Rate: {hr_value} bpm")

async def main():
    print("🔍 Scanning for Bluetooth devices for 10 seconds...")
    devices = await BleakScanner.discover(timeout=10.0)
    
    print("\n--- Discovered Devices ---")
    polar_device = None
    for d in devices:
        name = d.name or "Unknown"
        if "Polar" in name or "H10" in name or "Verity" in name or "Sense" in name:
            polar_device = d
            print(f"✅ Found Polar device: {name} [MAC: {d.address}]")
            
    print("--------------------------\n")
            
    if not polar_device:
        print("❌ No Polar device found. Please make sure it's discoverable.")
        sys.exit(1)
        
    print("🔄 Connecting...")
    
    async with BleakClient(polar_device.address) as client:
        print("✅ Connected!")
        print("📥 Subscribing to continuous Heart Rate data... (Press Ctrl+C to stop)")
        
        # Start receiving continuous notifications
        await client.start_notify(HEART_RATE_MEASUREMENT_UUID, hr_data_handler)
        
        try:
            # Keep the script running to collect data. 
            count = 0
            while True:
                await asyncio.sleep(1)
                count += 1
                if count % 5 == 0:
                    print("⌛ Still Waiting for heartbeat data from the strap...")
        except asyncio.CancelledError:
            pass
        except KeyboardInterrupt:
            print("\n🛑 Stopping...")
        finally:
            print("Disconnecting...")
            try:
                await client.stop_notify(HEART_RATE_MEASUREMENT_UUID)
            except Exception as e:
                print("Device already disconnected.")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
