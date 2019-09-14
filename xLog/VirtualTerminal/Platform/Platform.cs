using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace xLog
{
    public static class Platform
    {
        #region OS Testing
        internal static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        internal static bool IsWindows
        {
            get
            {
                PlatformID p = Environment.OSVersion.Platform;
                return (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows || p == PlatformID.WinCE);
            }
        }
        #endregion



        #region Win32 Constants

        private const UInt32 GENERIC_WRITE = 0x40000000;
        private const UInt32 GENERIC_READ = 0x80000000;
        private const UInt32 FILE_SHARE_READ = 0x00000001;
        private const UInt32 FILE_SHARE_WRITE = 0x00000002;
        private const UInt32 OPEN_EXISTING = 0x00000003;
        private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        // common errors
        private const UInt32 ERROR_SUCCESS = 0x0;
        private const UInt32 ERROR_ACCESS_DENIED = 0x5;
        private const UInt32 ERROR_INVALID_HANDLE = 0x6;

        private const UInt32 ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        private const UInt32 STD_INPUT_HANDLE = unchecked((uint)-10);
        private const UInt32 STD_OUTPUT_HANDLE = unchecked((uint)-11);
        private const UInt32 STD_ERROR_HANDLE = unchecked((uint)-12);

        private const UInt32 SW_HIDE = 0;
        private const UInt32 SW_SHOW = 5;

        private const UInt32 ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        #endregion



        public static bool Supports_VirtualTerminal()
        {
            if (IsLinux) return true;

            /*string keynelPath = Environment.ExpandEnvironmentVariables("%windir%/system32/kernel32.dll");
            Assembly kernel32 = AssemblyLoadContext.Default.LoadFromAssemblyName("kernel32");
            var types = kernel32.GetExportedTypes();
            var gcmode = kernel32.GetModule("GetConsoleMode");*/



            return false;
        }
    }
}
