using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Net;


namespace DSKey
{
	unsafe class MainClass
	{
		static Dictionary<Keys, int> buttonmap = new Dictionary<Keys, int> () {
			{ Keys.A, uinput.BTN_A },
			{ Keys.B, uinput.BTN_B },
			{ Keys.X, uinput.BTN_X },
			{ Keys.Y, uinput.BTN_Y },
			{ Keys.L, uinput.BTN_TL },
			{ Keys.R, uinput.BTN_TR },
			{ Keys.SELECT, uinput.BTN_SELECT },
			{ Keys.START, uinput.BTN_START },
			{ Keys.DUP, uinput.BTN_DPAD_UP },
			{ Keys.DDOWN, uinput.BTN_DPAD_DOWN },
			{ Keys.DLEFT, uinput.BTN_DPAD_LEFT },
			{ Keys.DRIGHT, uinput.BTN_DPAD_RIGHT }
		};

		const uint BUTTONS_MAGIC = 0xCAFEBABE;
		const int PORT = 19050;
		public static int fd;

		static void BroadcastThread()
		{

		}
		public static void Main (string[] args)
		{
			

			//Pre-checks
			if (!File.Exists ("/dev/uinput"))
				Failure ("/dev/uinput does not exist. uinput kernel module not loaded?");
			//Connect to DS
			var bcaddr = new IPEndPoint(IPAddress.Any, PORT);
			var udpc = new UdpClient ();
			udpc.Client.Bind (bcaddr);
			IPEndPoint remoteep = bcaddr;
			Console.WriteLine ("Waiting for connection...");
			byte[] data;
			while (true) {
				data = udpc.Receive (ref remoteep);
				string stringData = Encoding.ASCII.GetString(data, 0, data.Length);
				Console.WriteLine("received: {0}  from: {1}",
					stringData, remoteep.ToString());
				if (stringData == "ANNOUNCE:3dskey") {
					TcpClient client = null;
					NetworkStream stream = null;
					try {
						client = new TcpClient (((IPEndPoint)remoteep).Address.ToString (), PORT);
						stream = client.GetStream ();
					} catch (Exception ex) {
						Console.WriteLine ("Connection failed");
						continue;
					}
					var reader = new BinaryReader (stream);
					RunDevice (reader);
					//Clear announce buffer
					while(udpc.Available > 0) udpc.Receive(ref bcaddr);
					Console.WriteLine ("Disconnected");
					Console.WriteLine ("Waiting for connection...");
				}
			}
		}

		static void RunDevice(BinaryReader reader)
		{
			var kvals = (Keys[])Enum.GetValues(typeof(Keys));
			//int fd;
			fd = libc.open ("/dev/uinput", libc.O_WRONLY | libc.O_NONBLOCK);
			if (fd < 0) {
				Failure ("Failed to open /dev/uinput");
			}
			int ret;
			ret = libc.ioctl (fd, uinput.UI_SET_EVBIT, uinput.EV_KEY);
			if (ret < 0)
				Failure ("UI_SET_EVBIT ioctl failed", ret);
			ret = libc.ioctl (fd, uinput.UI_SET_EVBIT, uinput.EV_ABS);
			if (ret < 0)
				Failure ("UI_SET_EVBIT ioctl failed", ret);
			ret = libc.ioctl (fd, uinput.UI_SET_EVBIT, uinput.EV_SYN);
			if (ret < 0)
				Failure ("UI_SET_EVBIT ioctl failed", ret);
			foreach (var button in buttonmap.Values) {
				ret = libc.ioctl (fd, uinput.UI_SET_KEYBIT, button);
				if (ret < 0)
					Failure ("UI_SET_KEYBIT ioctl failed");
			}
			ret = libc.ioctl (fd, uinput.UI_SET_ABSBIT, uinput.ABS_RX);
			if (ret < 0)
				Failure ("UI_SET_ABSBIT ioctl failed");
			ret = libc.ioctl (fd, uinput.UI_SET_ABSBIT, uinput.ABS_RY);
			if (ret < 0)
				Failure ("UI_SET_ABSBIT ioctl failed");

			ret = libc.ioctl (fd, uinput.UI_SET_ABSBIT, uinput.ABS_X);
			if (ret < 0)
				Failure ("UI_SET_ABSBIT ioctl failed");
			ret = libc.ioctl (fd, uinput.UI_SET_ABSBIT, uinput.ABS_Y);
			if (ret < 0)
				Failure ("UI_SET_ABSBIT ioctl failed");
			var uidev = new uinput.uinput_user_dev ();
			uidev.SetName ("nintendo-3ds");
			uidev.id.bustype = uinput.BUS_USB;
			uidev.id.vendor = 0x34fe;
			uidev.id.product = 0xfedc;
			uidev.id.version = 1;
			//Axis
			uidev.absmin[uinput.ABS_RX] = -32768;
			uidev.absmax [uinput.ABS_RX] = 32768;
			uidev.absflat [uinput.ABS_RX] = 0;

			uidev.absmin [uinput.ABS_RY] = -32768;
			uidev.absmax [uinput.ABS_RY] = 32768;
			uidev.absflat [uinput.ABS_RY] = 0;

			//Make SDL happy
			uidev.absmin[uinput.ABS_X] = -1;
			uidev.absmax[uinput.ABS_X] = 1;
			uidev.absmin[uinput.ABS_Y] = -1;
			uidev.absmax[uinput.ABS_Y] = 1;
			//Create device
			ret = libc.write (fd, (IntPtr)(&uidev), (IntPtr)Marshal.SizeOf<uinput.uinput_user_dev> ());
			ret = libc.ioctl (fd, uinput.UI_DEV_CREATE);

			Console.WriteLine ("Device created. Press any key to exit");
			while (true)
			{
				try
				{
					var magic = reader.ReadUInt32();
					Keys held = 0, down = 0, up = 0;
					int dpx, dpy;
					if (magic == BUTTONS_MAGIC)
					{
						down = (Keys)reader.ReadUInt32();
						held = (Keys)reader.ReadUInt32();
						up = (Keys)reader.ReadUInt32();
						dpx = reader.ReadInt32();
						dpy = reader.ReadInt32();
						ProcessCirclePad(dpx, dpy);
					}
					foreach (var k in kvals)
					{
						if ((down & k) != 0)
							KeyDown(k);
						if ((up & k) != 0)
							KeyUp(k);
					}
				}
				catch (Exception ex)
				{
					break;
				}
			}

			ret = libc.ioctl (fd, uinput.UI_DEV_DESTROY);
			libc.close (fd);
		}

		static int currentx = 0;
		static int currenty = 0;
		const uint DEADZONE_RADIUS = 20;

		static int clamped(float f)
		{
			if (f < -32768)
				return -32768;
			if (f > 32768)
				return 32768;
			return (int)f;
		}
		static void ProcessCirclePad(int cpadx, int cpady)
		{
			int new_x = 0;
			int new_y = 0;

			if (cpadx > -DEADZONE_RADIUS && cpadx < DEADZONE_RADIUS)
				new_x = 0;
			else {
				float f = cpadx / (float)0x9C;
				new_x = clamped(f * 32768);
			}
			if (cpady > -DEADZONE_RADIUS && cpady < DEADZONE_RADIUS)
				new_y = 0;
			else {
				float f = -cpady / (float)0x9C;
				new_y = clamped (f * 32768);
			}

			//Write the Event
			bool write_syn = false;
			if (new_x != currentx) {
				currentx = new_x;
				var ev = new uinput.input_event ();
				ev.type = uinput.EV_ABS;
				ev.code = (ushort)uinput.ABS_RX;
				ev.value = new_x;
				libc.write (fd, (IntPtr)(&ev), (IntPtr)sz_ev);
				write_syn = true;
			}
			if (new_y != currenty) {
				currenty = new_y;
				var ev = new uinput.input_event ();
				ev.type = uinput.EV_ABS;
				ev.code = (ushort)uinput.ABS_RY;
				ev.value = new_y;
				libc.write (fd, (IntPtr)(&ev), (IntPtr)sz_ev);
				write_syn = true;
			}
			if (write_syn) {
				var syn = new uinput.input_event ();
				syn.type = uinput.EV_SYN;
				libc.write (fd, (IntPtr)(&syn), (IntPtr)sz_ev);
			}
		}

		static void KeyUp(Keys key)
		{
			Console.WriteLine("KEY_UP: {0}", key);
			int b;
			if (buttonmap.TryGetValue (key, out b)) {
				var ev = new uinput.input_event ();
				ev.type = uinput.EV_KEY;
				ev.code = (ushort)b;
				ev.value = 0;
				WriteEvent (ev);
			}
		}

		static void KeyDown(Keys key)
		{
			Console.WriteLine("KEY_DOWN: {0}", key);
			int b;
			if (buttonmap.TryGetValue (key, out b)) {
				var ev = new uinput.input_event ();
				ev.type = uinput.EV_KEY;
				ev.code = (ushort)b;
				ev.value = 1;
				WriteEvent (ev);
			}
		}

		static int sz_ev = Marshal.SizeOf<uinput.input_event> ();
		static void WriteEvent(uinput.input_event ev)
		{
			libc.write (fd, (IntPtr)(&ev), (IntPtr)sz_ev);
			var syn = new uinput.input_event ();
			syn.type = uinput.EV_SYN;
			libc.write (fd, (IntPtr)(&syn), (IntPtr)sz_ev);
		}

		static void Failure(string reason, int retval = -1)
		{
			Console.WriteLine(reason + " ({0}, {1})", retval, Marshal.GetLastWin32Error());
			Console.WriteLine ("Press any key to exit");
			Console.ReadKey (true);
			Environment.Exit (-1);
		}
	}

	[Flags]
	enum Keys : uint
	{
		A = 1,
		B = 1 << 1,
		SELECT = 1 << 2,
		START = 1 << 3,
		DRIGHT = 1 << 4,
		DLEFT = 1 << 5,
		DUP = 1 << 6,
		DDOWN = 1 << 7,
		R = 1 << 8,
		L = 1 << 9,
		X = 1 << 10,
		Y = 1 << 11,
		ZL = 1 << 14,
		ZR = 1 << 15,
		TOUCH = 1 << 20,
		CSTICK_RIGHT = 1 << 24,
		CSTICK_LEFT = 1 << 25,
		CSTICK_UP = 1 << 26,
		CSTICK_DOWN = 1 << 27,
		CPAD_RIGHT = 1 << 28,
		CPAD_LEFT = 1 << 29,
		CPAD_UP = 1 << 30,
		CPAD_DOWN = unchecked((uint)(1 << 31))
	}
}
