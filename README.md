# 3dskey
Use a 3DS as a controller on Linux

### Requirements:
* 3DS capable of running homebrew applications
* *uinput* kernel module
* Latest ctrulib from git (current release will not compile this)
* Mono

### Usage
* Run `make` in the 3ds folder, and copy the resulting files to /3ds/3dskey on your SD card
* Compile the DSKey project in MonoDevelop
* Run `mono DSKey.exe` on your Linux device
* Run 3dskey from the homebrew menu. The devices should connect to each-other

### Troubleshooting
#### /dev/uinput fails to open
Your user doesn't have the correct permissions to create an input device. Run the program with `sudo`
#### Stuck on "Waiting for connection..."
1. Make sure you don't have port 19050 UDP and TCP blocked

### Protocol
1. DSKey.exe listens on UDP port 19050
2. 3DS sends out a broadcast UDP packet with the contents "ANNOUNCE:3dskey" (15 bytes)
3. 3DS listens on TCP port 19050
4. DSKey.exe receives broadcast and connects to the 3DS on TCP port 19050
5. The 3DS sends the following packets upon state changes:

| Type | Value | Description|
|------|-------|-----------|
| int32  | `0xCAFEBABE` | Magic Number |
| uint32 | `hidKeysDown()` | Keys pressed |
| uint32 | `hidKeysHeld()` | Keys held |
| uint32 | `hidKeysUp()` | Keys released |
| int32 | `hidCircleRead()` x value | X-coordinate of the Circle Pad |
| int32 | `hidCircleRead()` y value | Y-coordinate of the Circle Pad |
