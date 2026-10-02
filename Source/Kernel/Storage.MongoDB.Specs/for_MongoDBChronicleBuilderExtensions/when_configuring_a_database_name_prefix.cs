// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_configuring_a_database_name_prefix : Specification
{
    IMongoClient _client;
    IHost _host;
    IHostBuilder _builder;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
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
                    },
                    Clustering = new Clustering { Type = ClusteringType.MongoDB }
                }));
                silo.Services.AddSingleton(clientManager);
                silo.Services.AddSingleton(Substitute.For<IJobTypes>());
                silo.Services.AddSingleton(Substitute.For<ICustomSerializers>());
            });
    }

    void Because()
    {
        _host = _builder.Build();
        var database = _host.Services.GetRequiredService<IDatabase>();
        database.GetEventStoreDatabase(new EventStoreName("Ada")).GetNamespaceDatabase(new EventStoreNamespaceName("Contoso"));
        database.GetReadModelDatabase(new EventStoreName("Ada"), EventStoreNamespaceName.Default);
        database.GetReadModelDatabase(new EventStoreName("Ada"), new EventStoreNamespaceName("Contoso"));
        _host.Services.GetRequiredService<IJobsStorage>().GetFor("Ada", "Contoso");
    }

    void Destroy() => _host?.Dispose();

    [Fact] void should_prefix_membership() => _host.Services.GetRequiredService<IOptions<MongoDBMembershipTableOptions>>().Value.DatabaseName.ShouldEqual("run_chronicle+main");
    [Fact] void should_prefix_reminders() => _host.Services.GetRequiredService<IOptions<MongoDBRemindersOptions>>().Value.DatabaseName.ShouldEqual("run_chronicle+main");
    [Fact] void should_prefix_global_storage() => _client.Received().GetDatabase("run_chronicle+main", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_prefix_event_store_storage() => _client.Received().GetDatabase("run_Ada+es", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_prefix_namespace_and_job_storage() => _client.Received(2).GetDatabase("run_Ada+es+Contoso", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_prefix_default_read_model_storage() => _client.Received().GetDatabase("run_Ada", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_prefix_named_read_model_storage() => _client.Received().GetDatabase("run_Ada+Contoso", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_never_resolve_an_unprefixed_database() => _client.DidNotReceive().GetDatabase(Arg.Is<string>(name => !name.StartsWith("run_", StringComparison.Ordinal)), Arg.Any<MongoDatabaseSettings>());
}
