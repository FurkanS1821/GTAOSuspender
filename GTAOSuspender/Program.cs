using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GTAOSuspender
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (string.IsNullOrEmpty(process.ProcessName))
            {
                return;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (string.IsNullOrEmpty(process.ProcessName))
            {
                return;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

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

        static void Main()
        {
            try
            {
                var pid = Process.GetProcessesByName("gta5")[0].Id;

                Console.WriteLine("Waiting 5 seconds before suspending GTA 5...");
                Thread.Sleep(5000);

                Console.WriteLine("Suspending...");
                SuspendProcess(pid);

                Console.WriteLine("Waiting 10 seconds...");
                Thread.Sleep(10000);

                Console.WriteLine("Resuming GTA 5...");
                ResumeProcess(pid);

                Console.WriteLine("Everything is complete.");
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Please make sure GTA 5 is running. We cannot suspend something that doesn't exist ;)");
                Console.WriteLine("Press ESC to exit, or anything else to retry.");

                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    return;
                }

                Main();
            }
        }

        [Flags]
        public enum ThreadAccess
        {
            TERMINATE = 0x1,
            SUSPEND_RESUME = 0x2,
            GET_CONTEXT = 0x8,
            SET_CONTEXT = 0x10,
            SET_INFORMATION = 0x20,
            QUERY_INFORMATION = 0x40,
            SET_THREAD_TOKEN = 0x80,
            IMPERSONATE = 0x100,
            DIRECT_IMPERSONATION = 0x200
        }
    }
}
