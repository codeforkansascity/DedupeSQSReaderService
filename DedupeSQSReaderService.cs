using Amazon; 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DedupeSQSReaderService
{
	//https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer
	public partial class DedupeSQSReaderService : ServiceBase
	{
		#region Stuff from sample you probably don't need to alter
		public enum ServiceState
		{
			SERVICE_STOPPED = 0x00000001,
			SERVICE_START_PENDING = 0x00000002,
			SERVICE_STOP_PENDING = 0x00000003,
			SERVICE_RUNNING = 0x00000004,
			SERVICE_CONTINUE_PENDING = 0x00000005,
			SERVICE_PAUSE_PENDING = 0x00000006,
			SERVICE_PAUSED = 0x00000007,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ServiceStatus
		{
			public int dwServiceType;
			public ServiceState dwCurrentState;
			public int dwControlsAccepted;
			public int dwWin32ExitCode;
			public int dwServiceSpecificExitCode;
			public int dwCheckPoint;
			public int dwWaitHint;
		};
		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
		private static SQSProcessor _sqsProcessor = new SQSProcessor();
		#endregion


		private static int _pollingMilliSeconds = 60000; //this will get calculated
		private static void LoadParameters()
		{
			int.TryParse(System.Configuration.ConfigurationManager.AppSettings["PollingSeconds"].ToString(),out Parameters.PollingSeconds);
			Parameters.EventSourceName = System.Configuration.ConfigurationManager.AppSettings["EventSourceName"].ToString();
			Parameters.LogToWriteTo = System.Configuration.ConfigurationManager.AppSettings["LogToWriteTo"].ToString();
			Parameters.ProcessToRun = System.Configuration.ConfigurationManager.AppSettings["ProcessToRun"].ToString();
			Parameters.ProcessToRunParameters = System.Configuration.ConfigurationManager.AppSettings["ProcessToRunParameters"].ToString();
			Parameters.SQSQueueURL = System.Configuration.ConfigurationManager.AppSettings["SQSQueueURL"].ToString();
			if (Parameters.SQSQueueURL.Contains("us-east-1"))
				Parameters.AppRegionEndpoint = RegionEndpoint.USEast1;
			if (Parameters.SQSQueueURL.Contains("us-west-1"))
				Parameters.AppRegionEndpoint = RegionEndpoint.USWest1;
			if (Parameters.SQSQueueURL.Contains("us-east-2"))
				Parameters.AppRegionEndpoint = RegionEndpoint.USWest2;
		}
		public DedupeSQSReaderService(string[] args)
		{
			InitializeComponent();
			LoadParameters();
			CreateEventSource(Parameters.EventSourceName);
			//if (args.Length > 0)
			//{
			//	int.TryParse(args[0], out Parameters.PollingSeconds);
			//}
			_pollingMilliSeconds = Parameters.PollingSeconds * 1000;
			Log($"Polling for SQS messages every {Parameters.PollingSeconds} seconds.", 105);

			//if (args.Length > 1)
			//{
			//	Parameters.ProcessToRun = args[1];
			//}
			Log($"Will run {Parameters.ProcessToRun} when SQS message received.", 106);

			//int i = 2;
			//while (args.Length>i)
			//{
			//	Parameters.ProcessToRunParameters += " " + args[i];
			//	i++;
			//}
			Log($"Process parameters = {Parameters.ProcessToRunParameters} ", 107);
			Log($"Process will read SQS Queue: {Parameters.SQSQueueURL} ", 108);
		}

		protected override void OnStart(string[] args)
		{
			// Update the service state to Start Pending.
			ServiceStatus serviceStatus = new ServiceStatus();
			serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
			serviceStatus.dwWaitHint = 100000;
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);

			Timer timer = new Timer();
			timer.Interval = _pollingMilliSeconds; // milliseconds
			timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
			timer.Start();

			// Update the service state to Running.
			serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);
		}
		public void OnTimer(object sender, ElapsedEventArgs args)
		{
			Log($"{Parameters.EventSourceName} checking for SQS messages at {DateTime.Now}", 110);
			_ = _sqsProcessor.Run(); //discard response
		}
		protected override void OnStop()
		{
			
		}
		public static void Log(string message, int eventId, EventLogEntryType eventType = EventLogEntryType.Information)
		{
			//Console.WriteLine($"Reading message : {message.Body}");
			using (EventLog eventLog = new EventLog(Parameters.LogToWriteTo))
			{
				eventLog.Source = Parameters.EventSourceName;
				eventLog.WriteEntry(message, eventType, eventId, 1);
			}
		}
		private string CreateEventSource(string currentAppName)
		{
			string eventSource = currentAppName;
			bool sourceExists;
			try
			{
				// searching the source throws a security exception ONLY if not exists!
				sourceExists = EventLog.SourceExists(eventSource);
				if (!sourceExists)
				{   // no exception until yet means the user as admin privilege
					EventLog.CreateEventSource(eventSource, Parameters.LogToWriteTo);
				}
			}
			catch
			{
				eventSource = Parameters.LogToWriteTo;
			}

			return eventSource;
		}
	}
}
