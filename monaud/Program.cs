using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

internal static class NativeMethods
{
    // Get the HWND of the currently active foreground window
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // Get the raw (UINT) Process ID (PID) from HWND
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // Get the PID in string format
    public static string GetProcessId(IntPtr hwnd)
    {
        hwnd = GetForegroundWindow();
        // TODO: Add function to detect all of the programs within startup
        if (hwnd == IntPtr.Zero) {
            Console.WriteLine("No foreground window detected");
            return "";
        }
        
        GetWindowThreadProcessId(hwnd, out uint pid);

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
    public static string GetProcessName(IntPtr hwnd)
    {
        hwnd = GetForegroundWindow();
        // TODO: Add different function to detect all of the programs within startup
        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("No foreground window detected");
            return "";
        }

        GetWindowThreadProcessId(hwnd, out uint pid);

        try
        {
            using Process process = Process.GetProcessById((int)pid);

            Console.WriteLine($"Window Handle: {hwnd}");
            Console.WriteLine($"Process PID:   {pid}");
            Console.WriteLine($"Process Name:  {process.ProcessName}");

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