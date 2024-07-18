using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PPTControl
{
    public partial class MainWindow : Window
    {
        private bool _isPPTShowing = false;
        private Process? _pptProcess;

        public MainWindow()
        {
            InitializeComponent();
            MovedWindow();
            this.Hide();

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void MoveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            CheckPowerPointInstance();
        }
        private void MovedWindow()
        {
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;
            double windowWidth = this.Width;
            double windowHeight = this.Height;

            double leftPosition = (screenWidth - windowWidth) / 2;
            double topPosition = 0.98*screenHeight - windowHeight;

            this.Left = leftPosition;
            this.Top = topPosition;
        }

        private void CheckPowerPointInstance()
        {
            Process[] processes = Process.GetProcessesByName("POWERPNT");
            if (processes.Length > 0)
            {
                _pptProcess = processes[0];
                _pptProcess.EnableRaisingEvents = true;
                _pptProcess.Exited += PowerPointExited;

                CheckPPTSlideShow();
            }
            else
            {
                HideWindow();
                _isPPTShowing = false;
            }
        }

        private void CheckPPTSlideShow()
        {
            bool isShowing = IsPPTShowMode();
            if (isShowing && !_isPPTShowing)
            {
                ShowWindow();
                MovedWindow();
                _isPPTShowing = true;
            }
            else if (!isShowing && _isPPTShowing)
            {
                HideWindow();
                _isPPTShowing = false;
            }
        }

        private bool IsPPTShowMode()
        {
            if (_pptProcess != null && !_pptProcess.HasExited)
            {
                foreach (IntPtr handle in EnumerateProcessWindowHandles(_pptProcess.Id))
                {
                    if (IsWindowClass(handle, "screenClass"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsWindowClass(IntPtr hWnd, string className)
        {
            const int maxChars = 256;
            var classText = new char[maxChars];

            if (GetClassName(hWnd, classText, maxChars) > 0)
            {
                string windowClass = new string(classText);
                return windowClass.Contains(className);
            }

            return false;
        }

        private void ShowWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Show();
                this.Topmost = true;
                this.Activate();
            });
        }

        private void HideWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Hide();
                this.Topmost = false;
            });
        }

        private void PowerPointExited(object? sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                HideWindow();
                _pptProcess = null;
                _isPPTShowing = false;
            });
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            SimulateKeyPress(VK_LEFT);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            SimulateKeyPress(VK_RIGHT);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            SimulateKeyPress(VK_ESC);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            SimulateKeyPress(VK_MENU);
        }

        private void SimulateKeyPress(byte keyCode)
        {
            if (_pptProcess != null)
            {
                IntPtr hWnd = _pptProcess.MainWindowHandle;
                SetForegroundWindow(hWnd);
                keybd_event(keyCode, 0, 0, UIntPtr.Zero);
                keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_LEFT = 0x25;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_ESC = 0x1B;
        private const byte VK_MENU = 0x5D;

        private static List<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            List<IntPtr> handles = new List<IntPtr>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                uint windowProcessId;
                GetWindowThreadProcessId(hWnd, out windowProcessId);

                if (windowProcessId == processId)
                {
                    handles.Add(hWnd);
                }

                return true;
            }, IntPtr.Zero);

            return handles;
        }
    }
}
