using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DedupeSQSReaderService
{
    public class SQSProcessor
    {
        private readonly IAmazonSQS _amazonSqs;
        public SQSProcessor()
        {
            _amazonSqs = new AmazonSQSClient(Parameters.AppRegionEndpoint);
        }

        public async Task Run()
        {
            ReceiveMessageResponse receiveMessageResponse = null;
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = Parameters.SQSQueueURL
            };
            try
            {
                receiveMessageResponse = await _amazonSqs.ReceiveMessageAsync(receiveMessageRequest);
            }
            catch (Exception ex)
            {
                DedupeSQSReaderService.Log($"{Parameters.EventSourceName} Error" + ex.Message, 304, EventLogEntryType.Error);
			}
            if (!receiveMessageResponse.Messages.Any())
            {
                return;
            }

            foreach (var message in receiveMessageResponse.Messages)
            {
                DedupeSQSReaderService.Log($"Reading message : {message.Body}", 201);

                try
                {
                    if (!string.IsNullOrWhiteSpace(Parameters.ProcessToRun))
                    {
                        DedupeSQSReaderService.Log($"Running {Parameters.ProcessToRun + " " + Parameters.ProcessToRunParameters}", 202);
						_ = ProcessAsyncHelper.ExecuteShellCommand(Parameters.ProcessToRun, Parameters.ProcessToRunParameters, 1000000); //discard response
                    }
                }
                catch (Exception ex)
				{
                    DedupeSQSReaderService.Log($"Error launching app : {ex.Message}", 304, EventLogEntryType.Error);

                }
                var deleteRequest = new DeleteMessageRequest { QueueUrl = Parameters.SQSQueueURL, ReceiptHandle = message.ReceiptHandle };
                await _amazonSqs.DeleteMessageAsync(deleteRequest);
            }
        }
    }
}