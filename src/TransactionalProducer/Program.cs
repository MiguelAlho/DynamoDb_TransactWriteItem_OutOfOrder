using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;

namespace TransactionalProducer
{
    record Event(
        long AggregateId,
        int Version,
        long SequenceNumber,
        byte[] Payload);

    record Transaction(
        List<Event> Events
        );

    internal static class Program
    {
        static long SequenceNumber = 0;
        static readonly int TotalAggregates = 5;

        static readonly byte[] MultiKiloBytesPayload = new byte[10000];
        static readonly byte[] SingleBytePayload = new byte[1];

        static readonly string TableName = "samples.dynamostreamsequenceverifier.eventstore";

        static async Task Main()
        {
            List<Transaction> transactions = CreateTransactionList();

            await PublishEventsToDynamoTable(transactions);
            
            Console.WriteLine($"done!");
        }

        private static IAmazonDynamoDB CreateDynamoDbClient()
        {
            Console.WriteLine("Creating client...");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var options = builder.Build().GetAWSOptions();
            var client = options.CreateServiceClient<IAmazonDynamoDB>();
            Console.WriteLine($"...client created");
            return client;
        }

        private static async Task PublishEventsToDynamoTable(List<Transaction> transactions)
        {
            IAmazonDynamoDB client = CreateDynamoDbClient();
            
            foreach (Transaction transaction in transactions)
            {
                var request = new TransactWriteItemsRequest()
                {
                    TransactItems = transaction.Events.Select(x => new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            TableName = TableName,
                            Item = new Dictionary<string, AttributeValue>
                            {
                                { "AggregateId", new AttributeValue { S = x.AggregateId.ToString() } },
                                { "Version", new AttributeValue { N = x.Version.ToString() } },
                                { "SequenceNumber", new AttributeValue { N = x.SequenceNumber.ToString() } },
                                { "Payload", new AttributeValue { S = System.Text.Encoding.UTF8.GetString(x.Payload) } }
                            }
                        }
                    }).ToList()
                };

                var response = await client.TransactWriteItemsAsync(request);

                Console.WriteLine($"Transaction sent with {request.TransactItems.Count} PUTs - {response.HttpStatusCode}");
            }
        }

        internal static List<Transaction> CreateTransactionList()
        {
            List<Transaction> transactions = new List<Transaction>();

            for (long agg = 0; agg < TotalAggregates; agg++)
            {
                transactions.Add(new Transaction(new List<Event>() { 
                    new Event(agg, 0, SequenceNumber++, MultiKiloBytesPayload),
                    new Event(agg, 1, SequenceNumber++, SingleBytePayload)
                }));
            }

            Console.WriteLine($"{TotalAggregates} aggregate transactions generated, each with 2 events.");

            var finalTransaction = new Transaction(new List<Event>());
            for (long agg = 0; agg < TotalAggregates; agg++)
            {
                finalTransaction.Events.Add(new Event(agg, 2, SequenceNumber++, SingleBytePayload));
            }
            transactions.Add(finalTransaction);

            Console.WriteLine($"Final transaction with {TotalAggregates} events appended. Total {transactions.Count} transactions.");

            return transactions;
        }
    }
}
