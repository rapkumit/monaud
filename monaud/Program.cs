using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static NativeMethods;

internal static class NativeMethods
{
    // Get the hWnd of the currently active foreground window
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    // Get the raw (UINT) Process ID (PID) from hWnd
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // Enum for the MonitorFromWindow flags
    public enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    // Get the handle of the monitor (HMONITOR) associated with a window
    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, MonitorOptions dwFlags);

    // Struct representing a rectangle
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // Struct containing information about a display monitor
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        // The size is 32 (CCHDEVICENAME)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    // API to get monitor information
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
}

class Helper
{
    public class Window
    {
        // Get the PID in string format
        public static string GetProcessId(IntPtr hWnd)
        {
            hWnd = GetForegroundWindow();
            // TODO: Add function to detect all of the programs within startup
            if (hWnd == IntPtr.Zero)
            {
                return "";
            }

            GetWindowThreadProcessId(hWnd, out uint pid);

            try
            {
                using Process process = Process.GetProcessById((int)pid);

                return pid.ToString();
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"Process {pid} is no longer running.");
                return "";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("Access denied to process information.");
                return "";
            }
        }

        // Get the Process Name (PN) from PID
        public static string GetProcessName(IntPtr hWnd)
        {
            hWnd = GetForegroundWindow();
            // TODO: Add different function to detect all of the programs within startup
            if (hWnd == IntPtr.Zero)
            {
                return "";
            }

            GetWindowThreadProcessId(hWnd, out uint pid);

            try
            {
                using Process process = Process.GetProcessById((int)pid);

                return process.ProcessName;
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"Process {pid} is no longer running.");
                return "";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("Access denied to process information.");
                return "";
            }
        }
    }

    public class Monitor
    {
        // Get monitor name from its handler (hMonitor)
        public static string GetMonitorName(IntPtr hMonitor)
        {
            if (hMonitor == IntPtr.Zero)
            {
                return "No Monitor";
            }

            MONITORINFOEX mi = new MONITORINFOEX();
            // Default behavior according to the docs
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                // This will return a name like "\\.\DISPLAY1"
                return mi.szDevice;
            }

            return "Unknown Monitor";
        }

        public static List<string> GetAllActiveMonitorDevices()
        {
            List<string> monitorNames = new List<string>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref RECT monitorRect, IntPtr data) =>
            {
                monitorNames.Add(GetMonitorName(hMonitor));
                return true;
            }, IntPtr.Zero);

            return monitorNames;
        }
    }

    public class Audio
    {
        private static string GetCommandLine(string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return $"Error (Exit Code {process.ExitCode}): {error}";
            }

            return output;
        }

        public static List<string> GetAllActivePlaybackDevices()
        {
            List<string> activePlaybackDevices = new List<string>();

            MMDeviceEnumerator mMDeviceEnumerator = new MMDeviceEnumerator();

            // Get only the active playback devices (might change in the future)
            var playbackDevices = mMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in playbackDevices)
            {
                // device.FriendlyName returns a string like "Speakers (Realtek(R) Audio)"
                activePlaybackDevices.Add(device.FriendlyName);
            }

            return activePlaybackDevices;
        }

        // DEPENDS ON: SoundVolumeCommandLine by NirSoft
        // p.s. Thanks NirSoft for the awesome application
        // Please do check em out https://www.nirsoft.net/utils/sound_volume_command_line.html >:0
        public static string SetDefaultAudioDevice(string audioDeviceName, string processName)
        {
            return GetCommandLine("cmd.exe", $"/c echo \"/SetAppDefault {audioDeviceName} all {processName}\"");
        }
    }
}

class Config
{
    public static Dictionary<string, string> BindMonitorToDisplay = new Dictionary<string, string>()
    {
        {"\\\\.\\DISPLAY1", "Speakers (Realtek(R) Audio)"},
        {"\\\\.\\DISPLAY6", "CABLE Input (VB-Audio Virtual Cable)"}
    };
}

class Engine
{   
    public static void Start(List<string> allActivePlaybackDevices, List<string> allMonitorDevices)
    {
        foreach (var item in allActivePlaybackDevices)
        {
            Console.WriteLine(item);
        }

        foreach (var item in allMonitorDevices)
        {
            Console.WriteLine(item);
        }
    }

    public static void Run(IntPtr hWnd, IntPtr hMonitor)
    {
        // Process Name/ID > (Monitor Name & Audio Device)
        Dictionary<string, string> stateHandler = new Dictionary<string, string>();

        while (true)
        {
            hWnd = NativeMethods.GetForegroundWindow();
            string processId = Helper.Window.GetProcessId(hWnd);
            string processName = Helper.Window.GetProcessName(hWnd);

            hMonitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            string monitorName = Helper.Monitor.GetMonitorName(hMonitor);

            var userPreference = Config.BindMonitorToDisplay;

            // "Is this a new process that isn't listed yet?"
            if (!stateHandler.ContainsKey(processName))
            {
                Console.WriteLine($"NEW: {processId} {processName} | {monitorName}");

                stateHandler.Add(processName, monitorName);
            }
            // "Okay it's in our list, but has the monitor changed since last time?"
            else if (stateHandler[processName] != monitorName)
            {
                Console.WriteLine($"MOVED: {processId} {processName} | {monitorName}");

                if (userPreference.TryGetValue(monitorName, out string audioDevice))
                {
                    Console.WriteLine(Helper.Audio.SetDefaultAudioDevice(audioDevice, processName));
                }

                stateHandler[processName] = monitorName;
            }

            Thread.Sleep(1000);
        }
    }
}

class Program
{
    public static void Main(string[] args)
    {
        IntPtr hWnd = NativeMethods.GetForegroundWindow();
        IntPtr hMonitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MonitorOptions.MONITOR_DEFAULTTONEAREST);

        List<string> allActivePlaybackDevices = Helper.Audio.GetAllActivePlaybackDevices();
        List<string> allActiveMonitorDevices = Helper.Monitor.GetAllActiveMonitorDevices();

        Engine engine = new Engine();

        Engine.Start(allActivePlaybackDevices, allActiveMonitorDevices);
        Engine.Run(hWnd, hMonitor);
    }
}
