using xLog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace UnitTests
{
    public static class CONSOLE
    {
        #region Win32 Imports

        [DllImport("kernel32.dll")]
        static extern UInt32 GetLastError();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, UInt32 nCmdShow);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(UInt32 nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateFileW(
              string lpFileName,
              UInt32 dwDesiredAccess,
              UInt32 dwShareMode,
              IntPtr lpSecurityAttributes,
              UInt32 dwCreationDisposition,
              UInt32 dwFlagsAndAttributes,
              IntPtr hTemplateFile
            );



        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "FreeConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, UInt32 mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out UInt32 mode);
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


        /// <summary>
        /// Sets up our console to be able to use colors
        /// </summary>
        private static void InitializeNewConsole()
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            UInt32 mode;
            if (!GetConsoleMode(handle, out mode))
            {
                int err = Marshal.GetLastWin32Error();
                return;
            }
            bool SupportsVTS = SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);

            if(!SupportsVTS)// fallback to the old ways
            {
                int err = Marshal.GetLastWin32Error();
                return;
            }
        }

        private static void CreateConsole(bool alwaysCreateNewConsole = true)
        {
            bool newConsole = false;
            bool consoleAttached = true;

            var attachResult = AttachConsole(ATTACH_PARENT_PROCESS);
            var lastError = Marshal.GetLastWin32Error();
            if (alwaysCreateNewConsole
                || (attachResult == 0
                && lastError != ERROR_ACCESS_DENIED))
            {
                newConsole = consoleAttached = AllocConsole() != 0;
            }
            else if (attachResult == 0 && 
                lastError == ERROR_ACCESS_DENIED && 
                GetConsoleWindow() == IntPtr.Zero)
            {/* We couldnt attach to the console, because we are already attached to one. And yet we also cannot get the ptr to its window handle. At this point just detatch from the console and make a new one! */
                FreeConsole();
                var freeErr = Marshal.GetLastWin32Error();
                AllocConsole();
                var allocErr = Marshal.GetLastWin32Error();
                newConsole = true;
            }

            if (consoleAttached)
            {
                InitializeOutStream();
                InitializeInStream();
            }

            if (newConsole)
            {
                InitializeNewConsole();
            }
        }

        public static void ShowConsoleWindow()
        {
            IntPtr handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                CreateConsole(false);
            }

            ShowWindow(handle, SW_SHOW);
        }

        public static void HideConsoleWindow()
        {
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        public static void ToggleConsoleWindow()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_HIDE);
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
            }
        }



        private static void InitializeOutStream()
        {
            var fs = CreateFileStream("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write, out SafeFileHandle File);
            if (fs != null)
            {
                var writer = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);

                SetStdHandle(STD_OUTPUT_HANDLE, File.DangerousGetHandle());
                SetStdHandle(STD_ERROR_HANDLE, File.DangerousGetHandle());
                //File.
            }
        }

        private static void InitializeInStream()
        {
            var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read, out SafeFileHandle File);
            if (fs != null)
            {
                Console.SetIn(new StreamReader(fs));
            }
        }

        private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode, FileAccess dotNetFileAccess, out SafeFileHandle File)
        {
            var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
            File = file;
            if (!file.IsInvalid)
            {
                var fs = new FileStream(file, dotNetFileAccess);
                return fs;
            }
            return null;
        }
    }
}
