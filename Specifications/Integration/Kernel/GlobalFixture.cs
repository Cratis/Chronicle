// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

public class GlobalFixture : IDisposable
{
    internal static readonly INetwork Network;

    static GlobalFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
    }

    public IContainer MongoDBContainer { get; }
    public Subject<ChangeStreamDocument<BsonDocument>> Changes { get; } = new();

    public GlobalFixture()
    {
        Directory.CreateDirectory("backups");

        MongoDBContainer = new ContainerBuilder()
            .WithImage("aksioinsurtech/mongodb")
            .WithPortBinding(27017)
            .WithHostname("mongo")
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(Network)
            .Build();

        MongoDBContainer.StartAsync().GetAwaiter().GetResult();

        StartChangeStreamReplication();
    }

    public void Dispose()
    {
        MongoDBContainer.ExecAsync(new[]
        {
            "mongodump",
            $"--archive=/backups/{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.tgz",
            "--gzip"
        }).GetAwaiter().GetResult();

        MongoDBContainer.StopAsync().GetAwaiter().GetResult();
#pragma warning disable CA2012 // Use ValueTasks correctly
        var disposeTask = MongoDBContainer.DisposeAsync();
        if (!disposeTask.IsCompleted)
        {
            disposeTask.GetAwaiter().GetResult();
        }
#pragma warning restore CA2012 // Use ValueTasks correctly
    }

    void StartChangeStreamReplication()
    {
        var mongoClient = new MongoClient($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}");
        var database = mongoClient.GetDatabase("cratis-shared");
        var changes = mongoClient.GetDatabase("test-changes");
        var changesCollection = changes.GetCollection<BsonDocument>("changes");

        _ = Task.Run(async () =>
        {
            var filter = Builders<ChangeStreamDocument<BsonDocument>>.Filter.In(
                new StringFieldDefinition<ChangeStreamDocument<BsonDocument>, string>("operationType"),
                new[] { "insert", "replace", "update", "delete" });

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Match(filter);

            var cursor = await database.WatchAsync(
                pipeline,
                new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });


            while (await cursor.MoveNextAsync())
            {
                if (!cursor.Current.Any()) continue;

                foreach (var document in cursor.Current)
                {
                    Changes.OnNext(document);
                    var changeDocument = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "collectionNamespace", document.CollectionNamespace.ToString() },
                        { "documentKey", document.DocumentKey },
                        { "operationType", document.OperationType.ToString() },
                        { "fullDocument", document.FullDocument }
                    };
                    await changesCollection.InsertOneAsync(changeDocument);
                }
            }
        });
    }
}
