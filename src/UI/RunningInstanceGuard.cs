using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenCivOne.UI
{
	/// <summary>
	/// Finds other running OpenCivOne processes. A game process that did not shut
	/// down cleanly keeps the executable locked, which prevents both starting a new
	/// copy and building the project until the process is killed.
	/// </summary>
	internal static class RunningInstanceGuard
	{
		private const string ProcessNamePrefix = "OpenCivOne";

		private const uint MessageBoxYesNo = 0x00000004;
		private const uint MessageBoxDefaultButton2 = 0x00000100;
		private const uint MessageBoxIconWarning = 0x00000030;
		private const uint MessageBoxIconError = 0x00000010;
		private const uint MessageBoxSetForeground = 0x00010000;
		private const uint MessageBoxTopMost = 0x00040000;

		private const int MessageBoxResultYes = 6;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int MessageBoxW(IntPtr handle, string text, string caption, uint type);

		/// <summary>
		/// Returns the other running game processes.
		/// </summary>
		public static List<Process> GetOtherInstances()
		{
			Process current = Process.GetCurrentProcess();
			List<Process> instances = new List<Process>();

			foreach (Process process in Process.GetProcesses())
			{
				try
				{
					if (process.Id == current.Id)
					{
						continue;
					}

					if (!process.ProcessName.StartsWith(ProcessNamePrefix, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					instances.Add(process);
				}
				catch
				{
					// The process exited while it was being inspected.
					process.Dispose();
				}
			}

			return instances;
		}

		/// <summary>
		/// If another instance is running, asks the user whether to force kill it.
		/// Returns true when no other instance is running any more.
		/// </summary>
		public static bool ConfirmAndStopOtherInstances()
		{
			List<Process> instances = GetOtherInstances();

			if (instances.Count == 0)
			{
				return true;
			}

			string description = string.Join(", ", instances.Select(instance =>
			{
				try
				{
					return $"{instance.ProcessName} (PID {instance.Id})";
				}
				catch
				{
					return "OpenCivOne";
				}
			}));

			string question = "OpenCivOne is already running:\n\n" +
				description + "\n\n" +
				"A previous copy may not have shut down cleanly and is keeping the executable locked.\n\n" +
				"Do you want to force kill it and continue?";

			if (ShowMessage(question, "OpenCivOne is already running",
				MessageBoxYesNo | MessageBoxDefaultButton2 | MessageBoxIconWarning | MessageBoxSetForeground | MessageBoxTopMost) != MessageBoxResultYes)
			{
				return false;
			}

			foreach (Process instance in instances)
			{
				try
				{
					instance.Kill(entireProcessTree: true);
					instance.WaitForExit(5000);
				}
				catch (Exception e)
				{
					ShowMessage(
						$"Could not stop {instance.ProcessName} (PID {instance.Id}).\n\n{e.Message}\n\n" +
						"Close it manually from Task Manager and start OpenCivOne again.",
						"Could not stop the running instance",
						MessageBoxIconError | MessageBoxSetForeground | MessageBoxTopMost);
				}
				finally
				{
					instance.Dispose();
				}
			}

			return GetOtherInstances().Count == 0;
		}

		/// <summary>
		/// Shows a native message box before Avalonia is initialized. On platforms
		/// without one, falls back to the console so the user still gets a prompt.
		/// </summary>
		private static int ShowMessage(string text, string caption, uint type)
		{
			if (OperatingSystem.IsWindows())
			{
				return MessageBoxW(IntPtr.Zero, text, caption, type);
			}

			Console.WriteLine(caption);
			Console.WriteLine(text);

			if ((type & MessageBoxYesNo) != 0)
			{
				Console.Write("Force kill it? [y/N] ");

				string? answer = Console.ReadLine();

				return (answer != null && answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) ?
					MessageBoxResultYes : 0;
			}

			return 0;
		}
	}
}
