// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBSchemaServices;

public class when_resolving_connections : Specification
{
    ServiceProvider _provider;
    IStorageConnectionResolution _resolution;

    void Establish()
    {
        var services = new ServiceCollection();
        MongoDBSchemaServices.Add(services, new ChronicleOptions { Storage = new Configuration.Storage { ConnectionDetails = "mongodb://localhost:27017" } });
        _resolution = Substitute.For<IStorageConnectionResolution>();
        _resolution.IsConfigured.Returns(true);
        _resolution.RequestFor(Arg.Any<ConnectionPurpose>(), Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => StorageConnectionRequests.For(WellKnownConnectionStorage.MongoDB, call.Arg<ConnectionPurpose>(), call.Arg<StorageConnectionKind>(), call.ArgAt<string>(2), call.ArgAt<string>(3)));
        _resolution.Resolve(Arg.Any<ConnectionPurpose>(), Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>()).Returns(ResolvedConnection.Unchanged);
        var clients = Substitute.For<IMongoDBClientManager>();
        clients.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(Substitute.For<IMongoClient>());
        services.AddSingleton(_resolution);
        services.AddSingleton(clients);
        _provider = services.BuildServiceProvider();
    }

    void Because() => _provider.GetRequiredService<IMongoDBStorageConnections>().GetDatabase(StorageConnectionKind.Namespace, "schema_ns", "orders", "default");

    [Fact] void should_request_the_schema_identity() => _resolution.Received().RequestFor(ConnectionPurpose.Schema, StorageConnectionKind.Namespace, "orders", "default");
    [Fact] void should_resolve_the_schema_identity() => _resolution.Received().Resolve(ConnectionPurpose.Schema, StorageConnectionKind.Namespace, "orders", "default");
    [Fact] void should_not_resolve_the_runtime_identity() => _resolution.DidNotReceive().Resolve(ConnectionPurpose.Runtime, StorageConnectionKind.Namespace, "orders", "default");

    void Destroy() => _provider.Dispose();
}
