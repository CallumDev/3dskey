/*
	Circle Pad example made by Aurelio Mannara for ctrulib
	Please refer to https://github.com/smealum/ctrulib/blob/master/libctru/include/3ds/services/hid.h for more information
	This code was modified for the last time on: 12/13/2014 2:20 UTC+1

	This wouldn't be possible without the amazing work done by:
	-Smealum
	-fincs
	-WinterMute
	-yellows8
	-plutoo
	-mtheall
	-Many others who worked on 3DS and I'm surely forgetting about
*/

#include <3ds.h>
#include <stdio.h>
#include <string.h>
#include <malloc.h>
#include <errno.h>
#include <stdarg.h>
#include <unistd.h>

#include <fcntl.h>

#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>

#include <3ds.h>

#define SOC_ALIGN 0x1000
#define SOC_BUFFERSIZE 0x100000
#define SO_BROADCAST 0x4
#define PORT 19050

static u32 *SOC_buffer = NULL;
s32 sock = -1, csock = -1;
s32 bcast_sock = -1;

const static char broadcast_msg[] = "ANNOUNCE:3dskey";

#define BUTTONS_MAGIC 0xCAFEBABE
int main(int argc, char **argv)
{
	u32 clientlen;
	struct sockaddr_in client;
	struct sockaddr_in server;
	struct sockaddr_in broadcastaddr;
	char temp[1026];
	static int hits = 0;
	u32 buttonspacket[6] = {
		BUTTONS_MAGIC, 0, 0, 0, 0, 0
	};
	// Initialize services
	gfxInitDefault();

	//Initialize console on top screen. Using NULL as the second argument tells the console library to use the internal console structure as current one
	consoleInit(GFX_TOP, NULL);

	SOC_buffer = (u32*)memalign(SOC_ALIGN, SOC_BUFFERSIZE);

	socInit(SOC_buffer, SOC_BUFFERSIZE);

	clientlen = sizeof(client);
	sock = socket(AF_INET, SOCK_STREAM, IPPROTO_IP);
	if (sock < 0) {
		socExit();
		gfxExit();
		return;
	}

	
	bcast_sock = socket(AF_INET, SOCK_DGRAM, 0);
	fcntl(bcast_sock, F_SETFL, fcntl(sock,  F_GETFL, 0) | O_NONBLOCK);
	int broadcastEnable = 0;
	int ret = setsockopt(bcast_sock, SOL_SOCKET, SO_BROADCAST, &broadcastEnable, sizeof(broadcastEnable));

	memset(&server, 0, sizeof(server));
	memset(&client, 0, sizeof(client));
	server.sin_family = AF_INET;
	server.sin_port = htons(PORT);
	server.sin_addr.s_addr = gethostid();

	memset(&broadcastaddr, 0, sizeof(broadcastaddr));
	broadcastaddr.sin_family = AF_INET;
	broadcastaddr.sin_port = htons(PORT);
	inet_pton(AF_INET, "255.255.255.255", &(broadcastaddr.sin_addr));



	u32 kDownOld = 0, kHeldOld = 0, kUpOld = 0; //In these variables there will be information about keys detected in the previous frame

	

	bind (sock, (struct sockaddr *) &server, sizeof (server));

	fcntl(sock, F_SETFL, fcntl(sock,  F_GETFL, 0) | O_NONBLOCK);

	listen(sock, 5);
	int was_connected = 0;
	printf("3dsKey\n TAP TO EXIT\n");
	printf("Attempting to connect\n");
	int frame_counter = 0;
	circlePosition oldPos;
	// Main loop
	int packet_number = 0;
	while (aptMainLoop())
	{
		//Scan all the inputs. This should be done once for each frame
		hidScanInput();

		if (csock < 0) {
			csock = accept (sock, (struct sockaddr *) &client, &clientlen);
			if (errno == EAGAIN) {
				csock == -1;
				if(frame_counter > 20) {
					frame_counter = 0;
				int sret = sendto(bcast_sock, broadcast_msg, strlen(broadcast_msg), 0, (struct sockaddr *)&broadcastaddr, sizeof(broadcastaddr));
				}
				was_connected = 0;
				frame_counter++;
			} 
			u32 kdwn = hidKeysDown();
			if (kdwn & KEY_TOUCH) break;
		} else {
			if(!was_connected) {
				printf("Connected!\n");
				was_connected = 1;
			}
		//hidKeysDown returns information about which buttons have been just pressed (and they weren't in the previous frame)
		u32 kDown = hidKeysDown();
		//hidKeysHeld returns information about which buttons have are held down in this frame
		u32 kHeld = hidKeysHeld();
		//hidKeysUp returns information about which buttons have been just released
		u32 kUp = hidKeysUp();

		if (kDown & KEY_TOUCH) break; // break in order to return to hbmenu

		circlePosition pos;
		//Read the CirclePad position
		hidCircleRead(&pos);

		//Let's not destroy the network
		if(kDown != kDownOld || kUp != kUpOld || kHeld != kHeldOld
			|| oldPos.dx != pos.dx || oldPos.dy != pos.dy) {
			buttonspacket[1] = kDown;
			buttonspacket[2] = kHeld;
			buttonspacket[3] = kUp;
			
			buttonspacket[4] = (u32)pos.dx;
			buttonspacket[5] = (u32)pos.dy;
			//block
			fcntl(csock, F_SETFL, fcntl(csock, F_GETFL, 0) & ~O_NONBLOCK);
			//send
			send(csock, buttonspacket, sizeof(u32) * 6, 0);
			//unblock
			fcntl(csock, F_SETFL, fcntl(sock,  F_GETFL, 0) | O_NONBLOCK);
			printf("Packet %d sent \n", packet_number++);
		}
		
		//Set keys old values for the next frame
		kDownOld = kDown;
		kHeldOld = kHeld;
		kUpOld = kUp;
		oldPos = pos;
		
		}
		// Flush and swap framebuffers
		gfxFlushBuffers();
		gfxSwapBuffers();

		//Wait for VBlank
		gspWaitForVBlank();
	}
	printf("Closing sockets..\n");
	close(csock);
	close(sock);
	close(bcast_sock);
	printf("Exiting\n");
	// Exit services
	socExit();
	printf("Sockets de-inited\n");
	gfxExit();
	return 0;
}
