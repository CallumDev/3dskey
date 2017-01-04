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
1. Make sure you don't have port 19050 UDP inbound blocked
2. Make sure you don't have port 68 TCP outbound blocked

