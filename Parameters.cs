using Amazon;

namespace DedupeSQSReaderService
{
	public static class Parameters
	{
		#region Constants you need to change
		internal static int PollingSeconds = 60;
		internal static string EventSourceName = "DeDupeSQSReader";
		internal static string LogToWriteTo = "Application";
		internal static string ProcessToRun = @"C:\Users\Administrator\AppData\Local\Programs\Python\Python39\python.exe";
		internal static string ProcessToRunParameters = "KCDigitalDrive_Vacc.py c4kc-cvax-deduplication c4kc-cvax-deduplication";
		//If you use just the queue name you get this error:
		//Invalid URI: The format of the URI could not be determined
		//_sqsQueueUrl = "cvax-dedupe-request-queue"; 
		//using arn gets error like "The request signature we calculated does not match the signature you provided.  Check your AWS Secret Access Key and signing method.
		//_sqsQueueUrl = "arn:aws:sqs:us-west-2:{your id here}:{your sqs queue name here}";
		internal static string SQSQueueURL = "https://sqs.us-west-2.amazonaws.com/{your id here}/{your sqs queue name here}";
		//wrong region: The specified queue does not exist for this wsdl version
		public static RegionEndpoint AppRegionEndpoint = RegionEndpoint.USWest2;

		#endregion
	}
}
