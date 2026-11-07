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
    }

    public class Audio
    {

    }

}

class Engine
{   
    public void Run()
    {
        // Process Name/ID > Monitor Name
        Dictionary<string, string> stateHandler = new Dictionary<string, string>(); // TODO: Consider 1 key and 2 values once audio routing comes in

        while (true)
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            string processID = Helper.Window.GetProcessId(hWnd);
            string processName = Helper.Window.GetProcessName(hWnd);

            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            string monitorName = Helper.Monitor.GetMonitorName(hMonitor);

            // "Is this a new process that isn't listed yet?"
            if (!stateHandler.ContainsKey(processName))
            {
                Console.WriteLine($"NEW: {processID} {processName} | {monitorName}");

                stateHandler.Add(processName, monitorName);
            }
            // "Okay it's in our list, but has the monitor changed since last time?"
            else if (stateHandler[processName] != monitorName)
            {
                Console.WriteLine($"MOVED: {processID} {processName} | {monitorName}");

                stateHandler[processName] = monitorName;

                // TODO: Audio routing
            }

            Thread.Sleep(1000);
        }
    }
}

class Program
{
    public static void Main(string[] args)
    {
        Engine engine = new Engine();

        engine.Run();
    }
}
