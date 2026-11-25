using System.Runtime.InteropServices;

namespace Web2App;

/// <summary>Shell interop for taskbar identity (AppUserModelID) and pin relaunch information.</summary>
internal static class NativeMethods
{
    private const int AsfwAny = -1;
    private const ushort VtLpwstr = 31;

    private static readonly Guid AppUserModelFormat = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    public static void SetProcessAppId(string appId) => SetCurrentProcessExplicitAppUserModelID(appId);

    public static void AllowAnyForegroundWindow() => AllowSetForegroundWindow(AsfwAny);

    public static void DestroyIconHandle(IntPtr handle) => DestroyIcon(handle);

    /// <summary>
    /// Gives a window its taskbar identity and what to run when it is pinned and relaunched.
    /// </summary>
    public static void SetWindowAppProperties(IntPtr hwnd, string appId, string relaunchCommand, string displayName, string? iconResource)
    {
        var iid = typeof(IPropertyStore).GUID;
        SHGetPropertyStoreForWindow(hwnd, ref iid, out var store);
        try
        {
            SetString(store, 2, relaunchCommand);       // System.AppUserModel.RelaunchCommand
            SetString(store, 4, displayName);           // System.AppUserModel.RelaunchDisplayNameResource
            if (iconResource is not null)
            {
                SetString(store, 3, iconResource);      // System.AppUserModel.RelaunchIconResource
            }

            SetString(store, 5, appId);                 // System.AppUserModel.ID, set last
            store.Commit();
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }
    }

    private static void SetString(IPropertyStore store, uint propertyId, string value)
    {
        var key = new PropertyKey { FormatId = AppUserModelFormat, PropertyId = propertyId };
        var variant = new PropVariant { VarType = VtLpwstr, Pointer = Marshal.StringToCoTaskMemUni(value) };
        try
        {
            store.SetValue(ref key, ref variant);
        }
        finally
        {
            PropVariantClear(ref variant);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(string appId);

    [DllImport("shell32.dll", PreserveSig = false)]
    private static extern void SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant variant);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);

        void GetAt(uint index, out PropertyKey key);

        void GetValue(ref PropertyKey key, out PropVariant value);

        void SetValue(ref PropertyKey key, ref PropVariant value);

        void Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public uint PropertyId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort VarType;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public IntPtr Pointer;
        public IntPtr Padding;
    }
}
