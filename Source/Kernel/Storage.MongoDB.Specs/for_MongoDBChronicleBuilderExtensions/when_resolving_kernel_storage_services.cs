// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.MongoDB.Schema;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_resolving_kernel_storage_services : Specification
{
    IHost _host;
    Exception _error;

    void Establish()
    {
        _host = Host.CreateDefaultBuilder()
            .AddCratisArc()
            .AddCratisMongoDB(options =>
            {
                options.Server = "mongodb://localhost:27017";
                options.Database = "chronicle";
            })
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering();
                silo.Services.AddTypeDiscovery();
                silo.Services.AddBindingsByConvention();
                silo.Services.AddSelfBindings();
                silo.AddChronicleToSilo(builder => builder.WithMongoDB(new ChronicleOptions
                {
                    Storage = new Configuration.Storage { ConnectionDetails = "mongodb://localhost:27017" }
                }));
            })
            .Build();
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _host.Services.GetRequiredService<IMongoDBStorageConnections>();
        _host.Services.GetRequiredService<MongoDBIndexProvisioner>();
        _host.Services.GetRequiredService<VerifyMongoDBIndexes>();
        _host.Services.GetRequiredService<Json.IExpandoObjectConverter>();
    });

    [Fact] void should_resolve_runtime_connections_and_schema_dependencies() => _error.ShouldBeNull();
    [Fact] void should_expose_only_the_runtime_connections_constructor_to_di() => typeof(MongoDBStorageConnections).GetConstructors().Length.ShouldEqual(1);

    void Destroy() => _host?.Dispose();
}
