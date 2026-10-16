// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Configuration.for_StorageConnectionResolution;

/// <summary>
/// Hosts that run convention binding before Chronicle is added to the silo, as the integration hosts do, get the
/// resolution registered by convention first. Convention binding cannot construct it, because it needs the kind of
/// storage and its options, so it has to leave it alone and let the MongoDB configuration register it.
/// </summary>
public class when_convention_binding_ran_before_mongodb_is_configured : Specification
{
    IHost _host;
    IStorageConnectionResolution _resolution;
    ConnectionRequest _request;

    void Establish() =>
        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .UseOrleans(silo =>
            {
                silo.Services.AddTypeDiscovery();
                silo.Services.AddBindingsByConvention();
                silo.Services.AddSelfBindings();
                silo.AddChronicleToSilo(chronicle => chronicle.WithMongoDB("mongodb://localhost:27017"));
                silo.Services.AddSingleton(Substitute.For<IJobTypes>());
                silo.Services.AddSingleton(Substitute.For<ICustomSerializers>());
            })
            .Build();

    void Because()
    {
        _resolution = _host.Services.GetRequiredService<IStorageConnectionResolution>();
        _request = _resolution.RequestFor(ConnectionPurpose.Runtime, StorageConnectionKind.Cluster);
    }

    void Destroy() => _host?.Dispose();

    [Fact] void should_resolve_the_resolution() => _resolution.ShouldNotBeNull();
    [Fact] void should_resolve_it_for_mongodb() => _request.Storage.ShouldEqual(WellKnownConnectionStorage.MongoDB);
}
