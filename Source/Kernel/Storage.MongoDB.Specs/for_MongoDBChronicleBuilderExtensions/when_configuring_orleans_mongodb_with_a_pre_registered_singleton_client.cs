// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_configuring_orleans_mongodb_with_a_pre_registered_singleton_client : Specification
{
    const string ConnectionString = "mongodb://configured-host:27017";

    IHostBuilder _hostBuilder = default!;
    IHost _host = default!;
    IMongoClient _preRegisteredMongoClient = default!;
    IMongoClient _orleansMongoClient = default!;
    Exception _error = default!;

    void Establish()
    {
        _preRegisteredMongoClient = Substitute.For<IMongoClient>();
        _hostBuilder = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = false;
            })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.Services.AddSingleton(_preRegisteredMongoClient);
                siloBuilder.AddChronicleToSilo(chronicleBuilder => chronicleBuilder.WithMongoDB(ConnectionString));
            });
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _host = _hostBuilder.Build();
        _orleansMongoClient = _host.Services.GetRequiredService<IMongoClientFactory>().Create("chronicle");
    });

    void Destroy() => _host?.Dispose();

    [Fact] void should_resolve_the_orleans_client_without_an_error() => _error.ShouldBeNull();
    [Fact] void should_return_the_pre_registered_mongo_client() => AssertAfterSuccessfulBuild(() => _orleansMongoClient.ShouldEqual(_preRegisteredMongoClient));

    void AssertAfterSuccessfulBuild(Action assertion)
    {
        _error.ShouldBeNull();
        assertion();
    }
}
