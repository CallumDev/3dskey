using System;
using System.Text;
using System.Runtime.InteropServices;
namespace DSKey
{
	public static class libc
	{
		public const int O_WRONLY = 1;
		public const int O_NONBLOCK = 0x4;

		[DllImport("libc")]
		public static extern int open(string path, int flags);

		[DllImport("libc", SetLastError = true)]
		public static extern int write (int fd, IntPtr pointer, IntPtr size);

		[DllImport("libc", SetLastError = true)]
		public static extern int ioctl(int fd, uint request, int arg0);

		[DllImport("libc", SetLastError = true)]
		public static extern int ioctl (int fd, uint request);

		[DllImport("libc")]
		public static extern void close(int fd);

		public struct timeval {
			public IntPtr tv_sec;
			public int tv_usec;
		}
	}

	public static class uinput
	{
		//Bus
		public const int BUS_USB = 0x03;
		//Buttons
		public const int BTN_A = 0x130;
		public const int BTN_B = 0x131;
		public const int BTN_X = 0x133;
		public const int BTN_Y = 0x134;
		public const int BTN_TL = 0x136;
		public const int BTN_TR = 0x137;
		public const int BTN_SELECT = 0x13a;
		public const int BTN_START = 0x13b;
		public const int BTN_DPAD_UP = 0x220;
		public const int BTN_DPAD_DOWN = 0x221;
		public const int BTN_DPAD_LEFT = 0x222;
		public const int BTN_DPAD_RIGHT = 0x223;
		//Axis
		public const int ABS_RX = 0x03;
		public const int ABS_RY = 0x04;
		public const int ABS_X = 0x00;
		public const int ABS_Y = 0x01;
		//Events
		public const int EV_SYN = 0x00;
		public const int EV_KEY = 0x01;
		public const int EV_ABS = 0x03;

		public const uint UI_SET_EVBIT = 0x40045564;
		public const uint UI_SET_KEYBIT = 0x40045565;
		public const uint UI_ABS_SETUP = 0x401c5504;
		public const uint UI_DEV_CREATE = 0x5501;
		public const uint UI_DEV_DESTROY = 0x5502;
		public const uint UI_SET_ABSBIT = 0x40045567;

		//Device creation
		public const int ABS_MAX = 0x3f;

		public const int UINPUT_MAX_NAME_SIZE = 80;
		public unsafe struct uinput_user_dev {
			public fixed byte name[UINPUT_MAX_NAME_SIZE];
			public input_id id;
			public int ff_effects_max;
			public fixed int absmax[ABS_MAX + 1];
			public fixed int absmin[ABS_MAX + 1];
			public fixed int absfuzz[ABS_MAX + 1];
			public fixed int absflat[ABS_MAX + 1];

			public void SetName(string _new) {
				fixed(byte* _n = name) {
					for (int i = 0; i < _new.Length; i++) {
						_n [i] = (byte)_new [i];
					}
					_n [_new.Length] = 0;
				}
			}
		}
		public struct input_id {
			public ushort bustype;
			public ushort vendor;
			public ushort product;
			public ushort version;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct input_event {
			public libc.timeval time;
			public ushort type;
			public ushort code;
			public int value;
		}
	}
}

