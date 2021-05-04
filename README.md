# DynamoDb TransactWriteItem Out Of Order Sample

A sample repo that demonstrates the out-of-order behavior that multiple items in the same partition and transaction reflected in a dynamostream

The `Transactional Producer` produces a set of `TransactWriteItem` calls to DynamoDb. The first N include a transaction each with 2 events. The first event in the PUT list has a much larger payload then the second one. My hypothesis is that the second event is written and completed faster then the first, therefore appearing first in the payload.

A final transaction is added with N events, one per event, to see the effect when different partition keys are present in the transaction set.

## SETUP

Use your favorite method to setup the infra (I used Terraformwhich I can't include here). You'll need:

- A DynamoDb table to write to
  - Partition Key is "AggregateId"
  - Sort Key is "Version"
- A lambda function with own role
  - associate a trigger to the dynamostream
  - should be able to publish logs to cloudwatch

Update producer's `appsettings.json` with region and profile info, and `Program.cs`line 29's `TableName` value.

Update Consumer's `aws-lambda-tools-defaults.json` file with necessary `function-name` value.

## Deploy lambda

Use `dotnet lambda` to simplify deployment: 

lambda tools can be installed through:
```
    dotnet tool install -g Amazon.Lambda.Tools
    dotnet tool update -g Amazon.Lambda.Tools
```

lambda can be deployed by:
```
    cd src\DynamoStreamLambdaConsumer
    dotnet lambda deploy-function <your function name>
```

## To execute

```
    cd src\TransactionalProducer
    dotnet run
```

Once completed, check the cloudwatch logs for output and ordering info.

## Verified behaviour 

Sample ooutput at start of processing:

```
2021-05-04T20:20:12.292+01:00	Stream processing complete.
2021-05-04T20:20:12.298+01:00	Beginning b699e189-82ed-4d89-9b3f-0bfcd66829c9 to process 2 records...
2021-05-04T20:20:12.444+01:00	Read 1: 0, 1 - 19382500000000018927626460
2021-05-04T20:20:12.444+01:00	Read 0: 0, 0 - 19382600000000018927626461
2021-05-04T20:20:12.461+01:00	Stream processing complete.
2021-05-04T20:20:12.462+01:00	END RequestId: b23a13d9-0d4e-4932-b7c7-3f80c919e3c1
2021-05-04T20:20:12.462+01:00	REPORT RequestId: b23a13d9-0d4e-4932-b7c7-3f80c919e3c1 Duration: 167.17 ms Billed Duration: 168 ms Memory Size: 512 MB Max Memory Used: 69 MB
```

Notice the bit :

```
Read <Seq number>: <AggregateId>, <Version> - 19382500000000018927626460
...
Read 1: 0, 1 - 19382500000000018927626460
Read 0: 0, 0 - 19382600000000018927626461
```

Event #1 is pushed to the stream before event #0, on the same aggregate (0). So though they were added in order to the PUT Items list in the TransactWriteRequest, they end up in the stream out of order.

This contradicts the docs, though it could be by design.

____ 

You'll need to clear the DB between executions.
