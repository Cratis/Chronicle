// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.MongoDB.Serialization;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

/// <summary>
/// Job state bypasses the storage connections, so the registered jobs storage has to match an existing database's
/// casing as well: a namespace stored as 'contoso' next to 'run_Ada+es+Contoso' opens that database for its
/// events and for its jobs alike.
/// </summary>
public class when_a_case_variant_of_a_namespace_database_exists : Specification
{
    IMongoClient _client;
    IHost _host;
    IHostBuilder _builder;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
        _client.ListDatabaseNames(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var cursor = Substitute.For<IAsyncCursor<string>>();
            cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
            cursor.Current.Returns(["run_Ada+es", "run_Ada+es+Contoso"]);
            return cursor;
        });
        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        _builder = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .AddCratisMongoDB(
                options =>
                {
                    options.Server = "mongodb://localhost:27017";
                    options.Database = "run_chronicle+main";
                },
                _ => { })
            .UseOrleans(silo =>
            {
                silo.Services.AddTypeDiscovery();
                silo.AddChronicleToSilo(chronicle => chronicle.WithMongoDB(new ChronicleOptions
                {
                    Storage = new Configuration.Storage
                    {
                        ConnectionDetails = "mongodb://localhost:27017",
                        DatabaseNamePrefix = "run_"
                    }
                }));
                silo.Services.AddSingleton(clientManager);
                silo.Services.AddSingleton(Substitute.For<IJobTypes>());
                silo.Services.AddSingleton(Substitute.For<ICustomSerializers>());
            });
    }

    void Because()
    {
        _host = _builder.Build();
        _host.Services.GetRequiredService<IDatabase>().GetEventStoreDatabase(new EventStoreName("Ada")).GetNamespaceDatabase(new EventStoreNamespaceName("contoso"));
        _host.Services.GetRequiredService<IJobsStorage>().GetFor("Ada", "contoso");
    }

    void Destroy() => _host?.Dispose();

    [Fact] void should_open_the_existing_database_for_events_and_jobs() => _client.Received(2).GetDatabase("run_Ada+es+Contoso", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_never_open_the_lowercase_variant() => _client.DidNotReceive().GetDatabase("run_Ada+es+contoso", Arg.Any<MongoDatabaseSettings>());
}
