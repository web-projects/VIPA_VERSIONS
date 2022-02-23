using Common.Config.Config;
using Common.Execution;
using Common.LoggerManager;
using Common.XO.Requests;
using Execution;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DEVICE_CORE
{
    class Program
    {
        #region --- Win32 API ---
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_SIZE = 0xF000;
        // window position
        const short SWP_NOMOVE = 0X2;
        const short SWP_NOSIZE = 1;
        const short SWP_NOZORDER = 0X4;
        const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        #endregion --- Win32 API ---

        static readonly DeviceActivator activator = new DeviceActivator();

        static bool applicationIsExiting = false;

        static private IConfiguration configuration;

        static async Task Main(string[] args)
        {
            SetupWindow();

            // setup working environment
            DirectoryInfo di = SetupEnvironment();

            // save current colors
            ConsoleColor foreGroundColor = Console.ForegroundColor;
            ConsoleColor backGroundColor = Console.BackgroundColor;

            // Device discovery
            string pluginPath = Path.Combine(Environment.CurrentDirectory, "DevicePlugins");

            IDeviceApplication application = activator.Start(pluginPath);

            await application.Run(new AppExecConfig
            {
                ForeGroundColor = foreGroundColor,
                BackGroundColor = backGroundColor
            }).ConfigureAwait(false);


            // VIPA VERSION
            await application.Command(LinkDeviceActionType.ReportVipaVersions).ConfigureAwait(false);
            await Task.Delay(15000);

            // IDLE SCREEN
            //await application.Command(LinkDeviceActionType.DisplayIdleScreen).ConfigureAwait(false);

            applicationIsExiting = true;

            application.Shutdown();

            // delete working directory
            DeleteWorkingDirectory(di);
        }

        static private DirectoryInfo SetupEnvironment()
        {
            DirectoryInfo di = null;

            // create working directory
            if (!Directory.Exists(Constants.TargetDirectory))
            {
                di = Directory.CreateDirectory(Constants.TargetDirectory);
            }

            // Get appsettings.json config - AddEnvironmentVariables() requires package: Microsoft.Extensions.Configuration.EnvironmentVariables
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // logger manager
            SetLogging();

            // Screen Colors
            SetScreenColors();

            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            return di;
        }

        static private void SetupWindow()
        {
            Console.BufferHeight = Int16.MaxValue - 1;
            //Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
            Console.CursorVisible = false;

            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero)
            {
                //DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }
        }

        static Modes.Execution ParseArguments(string[] args)
        {
            Modes.Execution mode = Modes.Execution.Console;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/C":
                    {
                        mode = Modes.Execution.Console;
                        break;
                    }

                    case "/S":
                    {
                        mode = Modes.Execution.StandAlone;
                        break;
                    }
                }
            }

            return mode;
        }

        static private void DeleteWorkingDirectory(DirectoryInfo di)
        {
            if (di == null)
            {
                di = new DirectoryInfo(Constants.TargetDirectory);
            }

            if (di != null)
            {
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                di.Delete();
            }
            else if (Directory.Exists(Constants.TargetDirectory))
            {
                di = new DirectoryInfo(Constants.TargetDirectory);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                Directory.Delete(Constants.TargetDirectory);
            }
        }

        static string[] GetLoggingLevels(int index)
        {
            return configuration.GetSection("LoggerManager:Logging").GetValue<string>("Levels").Split("|");
        }

        static void SetLogging()
        {
            try
            {
                string[] logLevels = GetLoggingLevels(0);

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = Directory.GetCurrentDirectory();
                    string filepath = path + "\\logs\\" + logname;

                    int levels = 0;
                    foreach (string item in logLevels)
                    {
                        foreach (LOGLEVELS level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);

                    Logger.info($"{Assembly.GetEntryAssembly().GetName().Name} ({Assembly.GetEntryAssembly().GetName().Version}) - LOGGING INITIALIZED.");
                }
            }
            catch (Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", e.Message);
            }
        }

        static void SetScreenColors()
        {
            try
            {
                // Set Foreground color
                Console.ForegroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("ForeGround"));

                // Set Background color
                Console.BackgroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("BackGround"));

                Console.Clear();
            }
            catch (Exception ex)
            {
                Logger.error("main: SetScreenColors() - exception={0}", ex.Message);
            }
        }

        static ConsoleColor GetColor(string color) => color switch
        {
            "BLACK" => ConsoleColor.Black,
            "DARKBLUE" => ConsoleColor.DarkBlue,
            "DARKGREEEN" => ConsoleColor.DarkGreen,
            "DARKCYAN" => ConsoleColor.DarkCyan,
            "DARKRED" => ConsoleColor.DarkRed,
            "DARKMAGENTA" => ConsoleColor.DarkMagenta,
            "DARKYELLOW" => ConsoleColor.DarkYellow,
            "GRAY" => ConsoleColor.Gray,
            "DARKGRAY" => ConsoleColor.DarkGray,
            "BLUE" => ConsoleColor.Blue,
            "GREEN" => ConsoleColor.Green,
            "CYAN" => ConsoleColor.Cyan,
            "RED" => ConsoleColor.Red,
            "MAGENTA" => ConsoleColor.Magenta,
            "YELLOW" => ConsoleColor.Yellow,
            "WHITE" => ConsoleColor.White,
            _ => throw new Exception($"Invalid color identifier '{color}'.")
        };
    }
}
