using System;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace Test
{

    class InterceptKeys

    {

        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYUP = 0x0101;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;

        private static IntPtr _hookID = IntPtr.Zero;


        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        public static void Main()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }


        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);


        static bool winDown = false;
        static StringBuilder sb;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;

                if (key == Keys.C && winDown)
                {
                    var process = Process.GetProcessesByName("chrome");
                    if (process.Length == 0)
                    {
                        Process.Start("chrome.exe");
                    }
                    else
                    {
                        SwitchToThisWindow(process.First(p => p.MainWindowHandle.ToInt32() != 0).MainWindowHandle, true);
                    }
                }

                winDown = key == Keys.LWin || key == Keys.RWin;
                Console.WriteLine($"Down: {vkCode} {key}");
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                //var otherThing = Marshal.ReadInt32(wParam);
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;


                if (key == Keys.LWin || key == Keys.RWin)
                    winDown = false;

                Console.WriteLine($"UP: {vkCode} {key}");
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);

        }


        private static void MonitorSearchBar()
        {
            var searchUI = Process.GetProcessesByName("SearchUI").FirstOrDefault();

            while (true)
            {
                if (AutomationElement.FocusedElement.Current.ProcessId == searchUI.Id)
                {
                    var focus = AutomationElement.FocusedElement;
                    var current = focus.Current;
                    Console.WriteLine($"{current.ClassName} {current.ProcessId} {current.GetType().Name}");

                    //var itemChildren = AutomationElement.FocusedElement.FindAll(TreeScope.Children, Condition.TrueCondition);

                    var props = focus.GetSupportedProperties().Where(x => x.ProgrammaticName.Contains("Value"));

                    foreach (var prop in props)
                    {
                        Console.WriteLine($"{prop.ProgrammaticName}, {focus.GetCurrentPropertyValue(prop)}");

                    }
                }

                Thread.Sleep(500);
            }
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool UnhookWindowsHookEx(IntPtr hhk);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}