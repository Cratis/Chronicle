// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_configuring_arc_and_orleans_mongodb_with_scope_validation : Specification
{
    const string ConnectionString = "mongodb://scope-safe-host:27018/?directConnection=true";

    IHostBuilder _hostBuilder = default!;
    IServiceCollection _services = default!;
    IHost _host = default!;
    Exception _error = default!;
    ServiceDescriptor _arcMongoClientDescriptor = default!;
    ServiceDescriptor _orleansMongoClientFactoryDescriptor = default!;
    IMongoClient _orleansMongoClient = default!;

    void Establish()
    {
        _hostBuilder = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;

                // Resolve the singleton under test explicitly. Eager validation would also construct unrelated
                // Chronicle services whose runtime collaborators are supplied only when the complete server starts.
                options.ValidateOnBuild = false;
            })
            .AddCratisMongoDB(
                options =>
                {
                    options.Server = ConnectionString;
                    options.Database = "arc";
                },
                _ => { })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.Services.AddTypeDiscovery();
                siloBuilder.Services.AddSingleton(Substitute.For<ITenantIdAccessor>());
                siloBuilder.AddChronicleToSilo(chronicleBuilder => chronicleBuilder.WithMongoDB(ConnectionString));
                _services = siloBuilder.Services;
            });
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _host = _hostBuilder.Build();
        _arcMongoClientDescriptor = _services.Last(_ => _.ServiceType == typeof(IMongoClient));
        _orleansMongoClientFactoryDescriptor = _services.Last(_ => _.ServiceType == typeof(IMongoClientFactory));
        _orleansMongoClient = _host.Services.GetRequiredService<IMongoClientFactory>().Create("chronicle");
    });

    void Destroy() => _host?.Dispose();

    [Fact] void should_resolve_the_orleans_client_without_a_scope_validation_error() => _error.ShouldBeNull();
    [Fact] void should_keep_the_arc_mongo_client_scoped() => AssertAfterSuccessfulBuild(() => _arcMongoClientDescriptor.Lifetime.ShouldEqual(ServiceLifetime.Scoped));
    [Fact] void should_register_the_orleans_mongo_client_factory_as_a_singleton() => AssertAfterSuccessfulBuild(() => _orleansMongoClientFactoryDescriptor.Lifetime.ShouldEqual(ServiceLifetime.Singleton));
    [Fact] void should_give_orleans_its_own_mongo_client() => AssertAfterSuccessfulBuild(_orleansMongoClient.ShouldBeOfExactType<MongoClient>);
    [Fact] void should_use_the_configured_server_for_orleans() => AssertAfterSuccessfulBuild(() => _orleansMongoClient.Settings.Server.Host.ShouldEqual("scope-safe-host"));
    [Fact] void should_use_the_configured_port_for_orleans() => AssertAfterSuccessfulBuild(() => _orleansMongoClient.Settings.Server.Port.ShouldEqual(27018));
    [Fact] void should_keep_direct_connection_enabled_for_orleans() => AssertAfterSuccessfulBuild(() => _orleansMongoClient.Settings.DirectConnection.ShouldBeTrue());

    void AssertAfterSuccessfulBuild(Action assertion)
    {
        _error.ShouldBeNull();
        assertion();
    }
}
