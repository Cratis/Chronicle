// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Json;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Cratis.Chronicle.Integration.OrleansInProcess;

public class OrleansFixture(GlobalFixture globalFixture) : WebApplicationFactory<Startup>
{
    string _name = string.Empty;

    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.UseCratisMongoDB(
            mongo =>
            {
                mongo.Server = "mongodb://localhost:27018";
                mongo.Database = "orleans";
            });

        builder.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Globals.JsonSerializerOptions);
            services.AddControllers();
        });
        builder.UseCratisChronicle();

        builder.UseOrleans(silo =>
            {
                silo
                    .UseLocalhostClustering()
                    .AddCratisChronicle(
                        options => options.EventStoreName = "sample",
                        _ => _.WithMongoDB());
            })
            .UseConsoleLifetime();

        // For some weird reason we need this https://stackoverflow.com/questions/69974249/no-app-configured-error-while-using-webapplicationfactory-for-running-integrat
        builder.ConfigureWebHostDefaults(b => b.Configure(app => { }));

        return builder;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot("Integration/OrleansInProcess");
    }

    public INetwork Network => GlobalFixture.Network;
    public MongoDBDatabase EventStoreDatabase => GlobalFixture.EventStore;
    public MongoDBDatabase EventStoreForNamespaceDatabase => GlobalFixture.EventStoreForNamespace;
    public MongoDBDatabase ReadModelsDatbase => GlobalFixture.ReadModels;

    public IEventStore EventStore => Services.GetRequiredService<IEventStore>();
    public IChronicleClient ChronicleClient => Services.GetRequiredService<IChronicleClient>();
    public IEventStoreStorage EventStoreStorage => Services.GetRequiredService<IStorage>().GetEventStore("sample");
    public IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(Concepts.EventStoreNamespaceName? namespaceName = null) => EventStoreStorage.GetNamespace(namespaceName ?? Concepts.EventStoreNamespaceName.Default);
    public IEventSequenceStorage GetEventLogStorage(Concepts.EventStoreNamespaceName? namespaceName = null) => GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(Concepts.EventSequences.EventSequenceId.Log);

    public GlobalFixture GlobalFixture { get; } = globalFixture;

    public void SetName(string name) => _name = name;

    protected override void Dispose(bool disposing)
    {
        GlobalFixture.PerformBackup(_name);
        GlobalFixture.RemoveAllDatabases().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }
}
