using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace GTAOSuspender
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private static readonly string _warningFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTAOSuspender", "warningShown");

        private static void Suspend(this Process process)
        {
            if (string.IsNullOrEmpty(process.ProcessName))
            {
                return;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(2, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        private static void Resume(this Process process)
        {
            if (string.IsNullOrEmpty(process.ProcessName))
            {
                return;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(2, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                int suspendCount;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        private static void Main(string[] args)
        {
            var help = new List<string>
            {
                "help",
                "--help",
                "-?",
                "/?"
            };

            if (args.Length > 0 && help.Contains(args[0].ToLower()))
            {
                PrintHelp();
                return;
            }

            if (args.Length > 0 && args.Length < 3 && args[0].ToLower() == "repeat")
            {
                if (args.Length == 1 || !uint.TryParse(args[1], out var delay))
                {
                    delay = 15;
                }

                if (!File.Exists(_warningFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"WARNING: You've selected to repeat this process every {delay} minutes.");
                    Console.WriteLine("Me, the developer cannot be kept responsible for any mistimed suspensions.");
                    Console.WriteLine("If you are going to play a multi-playered job/heist/mission etc., you had better close this program first.");
                    Console.WriteLine("Do you agree? (Y/N)");

                    requestKey:

                    switch (Console.ReadKey(true).Key)
                    {
                        case ConsoleKey.Y:
                            Directory.CreateDirectory(Path.GetDirectoryName(_warningFilePath));
                            File.Create(_warningFilePath);
                            Console.ResetColor();
                            Console.WriteLine("Continuing execution.");
                            break;
                        case ConsoleKey.N:
                            Console.ResetColor();
                            Console.WriteLine("Quitting in 3 seconds.");
                            Thread.Sleep(3000);
                            return;
                        default:
                            goto requestKey;
                    }
                }

                while (true)
                {
                    DoConstantSuspendAndResume(false);

                    Console.WriteLine($"Waiting {delay} minutes...");
                    Thread.Sleep((int)(1000000 * delay));
                }
            }

            if (!args.Any())
            {
                DoConstantSuspendAndResume();
                return;
            }

            PrintHelp();
            Console.WriteLine("Continuing execution thinking you were to use this once.");
            DoConstantSuspendAndResume();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Wrong arguments.");
            Console.WriteLine("\"GTAOSuspender.exe\": do suspension once and close");
            Console.WriteLine("\"GTAOSuspender.exe repeat x\": Do suspension every x minutes (Default: 15).");
        }

        private static void DoConstantSuspendAndResume(bool error = true)
        {
            restart:
            var pList = Process.GetProcessesByName("gta5");
            if (!pList.Any())
            {
                if (!error)
                {
                    Console.WriteLine("GTA 5 not found. Skipping this time.");
                    return;
                }

                Console.WriteLine("Please make sure GTA V is running. We cannot suspend something that doesn't exist ;)");
                Console.WriteLine("Press ESC to exit, or anything else to retry.");

                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    return;
                }

                goto restart;
            }

            var process = pList[0];

            Console.WriteLine("Waiting 5 seconds before suspending GTA V...");
            Thread.Sleep(5000);

            Console.WriteLine("Suspending...");
            process.Suspend();

            Console.WriteLine("Waiting 10 seconds...");
            Thread.Sleep(10000);

            Console.WriteLine("Resuming GTA V...");
            process.Resume();

            Console.WriteLine("Everything is complete.");
        }
    }
}
