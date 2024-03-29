﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ChangeDisplaySettingsEx;

class Program
{
    // Структура DISPLAY_DEVICE
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)] public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        [MarshalAs(UnmanagedType.U4)] public DisplayDeviceStateFlags StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    // Флаги DISPLAY_DEVICE.StateFlags
    [Flags()]
    public enum DisplayDeviceStateFlags : int
    {
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        PrimaryDevice = 0x4,
        MirroringDriver = 0x8,
        VGACompatible = 0x10,
        Removable = 0x20,
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;

    // Функции WinAPI
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
        uint dwflags, IntPtr lParam);

    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;
        private const int CCHDEVICENAME_SIZEOF = 64;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME_SIZEOF)]
        public string dmDeviceName;

        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;

        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    // Перечисление ScreenOrientation
    public enum ScreenOrientation : int
    {
        DMDO_DEFAULT = 0,
        DMDO_90 = 1,
        DMDO_180 = 2,
        DMDO_270 = 3
    }

    static void Main()
    {
        Log("Application is running.");

        while (true)
        {
            bool isNewInstance;
            using (Mutex mutex = new Mutex(true, "ChangeDisplaySettingsEx", out isNewInstance))
            {
                if (isNewInstance)
                {
                    ShowWindow(Process.GetCurrentProcess().MainWindowHandle, SW_HIDE);

                    var appName = AppDomain.CurrentDomain.FriendlyName;
                    var appPath = Process.GetCurrentProcess().MainModule!.FileName;

                    var rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rk!.SetValue(appName, appPath);

                    var d = new DISPLAY_DEVICE();
                    d.cb = Marshal.SizeOf(d);

                    // Проверяем, сколько дисплеев подключено
                    var i = 0;
                    var n = 0;
                    while (EnumDisplayDevices(null, (uint) i, ref d, 0))
                    {
                        if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) ==
                            DisplayDeviceStateFlags.AttachedToDesktop)
                        {
                            n++;
                            Log($"Display {i}: {d.DeviceName} {d.DeviceString} {d.DeviceID} {d.DeviceKey}");
                        }

                        i++;
                    }

                    Log($"Total: {n} display(s)");

                    // Если подключен только один дисплей
                    if (n == 0)
                    {
                        DEVMODE dm = new DEVMODE();
                        dm.dmSize = (ushort) Marshal.SizeOf(typeof(DEVMODE));
                        var result = ChangeDisplaySettingsEx("\\\\.\\DISPLAY1", ref dm, IntPtr.Zero, 0, IntPtr.Zero);

                        if (!result)
                        {
                            Log(
                                $"{DateTime.Now}: Error ChangeDisplaySettingsEx to Display 1 (Default display). Error code: {result}\n");
                        }
                        else
                        {
                            Log($"{DateTime.Now}: Success ChangeDisplaySettingsEx to Display 1 (Default display).\n");
                        }
                    }
                }
                else
                {
                    Log("Application instance is already running.");
                }

                Log("Application finished.");
            }
            
            Thread.Sleep(10000);
        }

        static void Log(string message)
        {
            string logMessage = string.Format("{0} {1}: {2}", DateTime.Now.ToShortDateString(),
                DateTime.Now.ToLongTimeString(), message);
            using (StreamWriter writer =
                   new StreamWriter($"changeDisplaySettingsEx_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_logs.txt", true))
            {
                writer.WriteLine(logMessage);
            }
        }
    }
}