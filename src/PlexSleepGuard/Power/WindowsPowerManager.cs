using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PlexSleepGuard.Power;

public sealed class WindowsPowerManager : IPowerManager
{
    private const uint PowerRequestContextVersion = 0;
    private const uint PowerRequestContextSimpleString = 0x1;
    private const uint PowerRequestSystemRequired = 0;

    public IDisposable AcquireSystemRequired(string reason)
    {
        var context = new PowerRequestContext
        {
            Version = PowerRequestContextVersion,
            Flags = PowerRequestContextSimpleString,
            Reason = reason
        };
        var handle = PowerCreateRequest(ref context);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "PowerCreateRequest failed.");
        }

        try
        {
            if (!PowerSetRequest(handle, PowerRequestSystemRequired))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "PowerSetRequest failed.");
            }

            return new PowerRequestLease(handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePowerRequestHandle PowerCreateRequest(ref PowerRequestContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(SafePowerRequestHandle powerRequest, uint requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerClearRequest(SafePowerRequestHandle powerRequest, uint requestType);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PowerRequestContext
    {
        public uint Version;
        public uint Flags;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Reason;
    }

    private sealed class PowerRequestLease : IDisposable
    {
        private SafePowerRequestHandle? handle;

        public PowerRequestLease(SafePowerRequestHandle handle) => this.handle = handle;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref handle, null);
            if (current is null || current.IsInvalid)
            {
                current?.Dispose();
                return;
            }

            try
            {
                if (!PowerClearRequest(current, PowerRequestSystemRequired))
                {
                    // There is no logger at this native boundary; Dispose still closes the handle.
                }
            }
            finally
            {
                current.Dispose();
            }
        }
    }

    private sealed class SafePowerRequestHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePowerRequestHandle() : base(true) { }

        protected override bool ReleaseHandle() => CloseHandle(handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
