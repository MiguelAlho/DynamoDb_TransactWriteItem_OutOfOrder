using System;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DynamoStreamLambdaConsumer
{
    public class Function
    {
        static Guid instanceId = Guid.NewGuid();

        public void FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            context.Logger.LogLine($"Beginning {instanceId} to process {dynamoEvent.Records.Count} records...");

            if (dynamoEvent.Records != null)
            {
                foreach (var record in dynamoEvent.Records)
                {
                    if (record.EventName.Value == "INSERT")
                    {
                        Document doc = Document.FromAttributeMap(record.Dynamodb.NewImage);

                        try
                        {

                            var @event = new Event(
                                doc["AggregateId"].AsLong(),
                                doc["Version"].AsInt(),
                                doc["SequenceNumber"].AsLong(),
                                record.Dynamodb.SequenceNumber
                            );

                            context.Logger.LogLine($"Read {@event.EventSequenceNumber}: {@event.AggregateId}, {@event.Version} - {@event.StreamSequenceNumber}");
                        }
                        catch(Exception ex)
                        {
                            context.Logger.LogLine($"{record.EventName.Value} skipped due to exception {ex.Message}");
                        }
                    }
                    else
                    {
                        context.Logger.LogLine($"{record.EventName.Value} skipped");
                    }

                }
            }

            context.Logger.LogLine("Stream processing complete.");
        }
    }

    public class Event
    {
        public Event(
            long AggregateId,
            int Version,
            long EventSequenceNumber,
            string StreamSequenceNumber)
        {
            this.AggregateId = AggregateId;
            this.Version = Version;
            this.EventSequenceNumber = EventSequenceNumber;
            this.StreamSequenceNumber = StreamSequenceNumber;
        }

        public long AggregateId { get; }
        public int Version { get; }
        public long EventSequenceNumber { get; }
        public string StreamSequenceNumber { get; }
    }
}