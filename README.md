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

You'll need to clear the DB between executions.
