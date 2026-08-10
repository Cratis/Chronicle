// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_configuring_orleans_mongodb_with_a_pre_registered_client_factory : Specification
{
    const string ConnectionString = "mongodb://configured-host:27017";

    IHostBuilder _hostBuilder = default!;
    IHost _host = default!;
    IMongoClientFactory _preRegisteredMongoClientFactory = default!;
    IMongoClientFactory _resolvedMongoClientFactory = default!;
    IMongoClient _preRegisteredMongoClient = default!;
    IMongoClient _orleansMongoClient = default!;
    Exception _error = default!;

    void Establish()
    {
        _preRegisteredMongoClient = Substitute.For<IMongoClient>();
        _preRegisteredMongoClientFactory = Substitute.For<IMongoClientFactory>();
        _preRegisteredMongoClientFactory.Create(Arg.Any<string>()).Returns(_preRegisteredMongoClient);
        _hostBuilder = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = false;
            })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.Services.AddScoped<IMongoClient>(_ => Substitute.For<IMongoClient>());
                siloBuilder.Services.AddSingleton(_preRegisteredMongoClientFactory);
                siloBuilder.AddChronicleToSilo(chronicleBuilder => chronicleBuilder.WithMongoDB(ConnectionString));
            });
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _host = _hostBuilder.Build();
        _resolvedMongoClientFactory = _host.Services.GetRequiredService<IMongoClientFactory>();
        _orleansMongoClient = _resolvedMongoClientFactory.Create("chronicle");
    });

    void Destroy() => _host?.Dispose();

    [Fact] void should_resolve_the_orleans_client_without_an_error() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_pre_registered_mongo_client_factory() => AssertAfterSuccessfulBuild(() => _resolvedMongoClientFactory.ShouldEqual(_preRegisteredMongoClientFactory));
    [Fact] void should_return_the_client_from_the_pre_registered_factory() => AssertAfterSuccessfulBuild(() => _orleansMongoClient.ShouldEqual(_preRegisteredMongoClient));

    void AssertAfterSuccessfulBuild(Action assertion)
    {
        _error.ShouldBeNull();
        assertion();
    }
}
