using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TorShield
{
    class Program
    {
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        static bool settingsReturn, refreshReturn;
        //variables
        public static string line = new String('-', 30);
        public static string on_command = "";
        public static string off_command = "";
        public static string address = "127.0.0.1"; //feel free to change
        public static string http_port = "9090"; //feel free to change
        public static string port = "9050"; //value copied from torrc
        public static string shell = "";
        public static string data = "";
        public static Process tor;
        //end of variables

        static Process RunCmd(string command)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = command,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            return process;
        }

        public static void RunTor(string torpath)
        {
            tor = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = torpath,
                    Arguments = String.Format("--HTTPTunnelPort {0}", http_port),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            tor.Start();
            if (tor.HasExited)
            {
                Console.WriteLine(line + Environment.NewLine + "An error has occured" + Environment.NewLine + line);
                Console.Write(tor.StandardOutput.ReadToEnd());
            }
            tor.WaitForExit();
            /*Process output = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    //Arguments = "/c", //.Replace("\n", "^echo")
                    RedirectStandardOutput = false,
                    RedirectStandardInput = false,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                }
            };
            output.Start();
            StreamWriter sw = output.StandardInput;
            StreamReader sr = output.StandardOutput;
            sw.WriteLine(tor_log);
            sr.Close();*/
            //tor.OutputDataReceived += (sender, args) => data += args.Data;
            // tor.BeginOutputReadLine();
            /*while (!data.Contains("100%"))
            {
                Console.Write(".");
                System.Threading.Thread.Sleep(1000);
            }*/
        }
        static void SetProxy(bool enabled)
        {
            if (enabled)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    RunCmd(String.Format("netsh winhttp set proxy proxy-server='socks=localhost:{0}' bypass-list='localhost'", port)); //not working?

                    RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                    registry.SetValue("ProxyEnable", 1);
                    registry.SetValue("ProxyServer", address + ":" + http_port);
                    settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                    refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
                }
                else
                {
                    RunCmd(on_command).Start();
                }
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                    registry.SetValue("ProxyEnable", 0);
                    registry.SetValue("ProxyServer", "");
                    settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                    refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
                }
                else
                {
                    RunCmd(off_command).Start();
                }
            }
        }
        static void Main(string[] args)
        {
            try
            {
                //var webclient = new System.Net.WebClient();
                //webclient.DownloadFile("downloadlink", "tor");
                Console.Write("Enter path of your tor binary: ");
                string torpath = Console.ReadLine();
                Console.Write(line + Environment.NewLine + "Enabling proxy" + Environment.NewLine + line);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    shell = "cmd.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    shell = Environment.GetEnvironmentVariable("SHELL");
                    on_command = String.Format("networksetup -setsocksfirewallproxy wi-fi {0} {1}; networksetup -setsocksfirewallproxystate wi-fi on", address, port);
                    off_command = "networksetup -setsocksfirewallproxystate wi-fi off";
                }
                else
                {
                    shell = Environment.GetEnvironmentVariable("SHELL");
                    on_command = String.Format("export http_proxy=socks5://{0}:{1} https_proxy=socks5://{0}:{1} ftp_proxy=socks5://{0}:{1}", address, port);
                    off_command = String.Format("unset http_proxy unset https_proxy unset ftp_proxy");
                }
                Console.Write(Environment.NewLine + line + Environment.NewLine + "Starting TOR" + Environment.NewLine + line + Environment.NewLine);
                new Task(() => { RunTor(torpath); }).Start();
                Thread.Sleep(1000); //wait for error to come up, otherwise dont print anything
                SetProxy(true);
                Console.Write(line + Environment.NewLine + "All set, dropping to shell" + Environment.NewLine + line + Environment.NewLine);
                while (true)
                {
                    Console.Write("> ");
                    string command = Console.ReadLine();
                    if (command == "disable")
                    {
                        SetProxy(false);
                        Console.WriteLine("Proxy disabled");
                    }
                    else if (command == "enable")
                    {
                        SetProxy(true);
                        Console.WriteLine("Proxy enabled");
                    }
                    else if (command == "exit")
                    {
                        if (!tor.HasExited)
                        {
                            tor.Kill();
                        }
                        SetProxy(false);
                        Environment.Exit(0);
                    }
                    else if (command == "info")
                    {
                        Console.WriteLine("Made by TheDebianGuy in .net core");
                        Console.WriteLine("I encourage you to make and support open source software as it helps keeping the internet free and secure");
                        Console.WriteLine("License: Attribution-NonCommercial-Share Alike 4.0 International");
                    }
                    else
                    {
                        Console.WriteLine("Invalid command");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Following error has occured during runtime: " + e.Message);
                Thread.Sleep(3000);
                Environment.Exit(1);
            }
        }
    }
}
