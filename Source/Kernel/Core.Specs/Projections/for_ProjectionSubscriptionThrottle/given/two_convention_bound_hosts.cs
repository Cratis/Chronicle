// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.MongoDB.Serialization;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Projections.for_ProjectionSubscriptionThrottle.given;

/// <summary>
/// Builds real convention-bound service providers without starting silos or connecting to MongoDB.
/// </summary>
public class two_convention_bound_hosts : Specification
{
    protected IHost _first;
    protected IHost _second;

    void Establish()
    {
        _first = BuildHost();
        _second = BuildHost();
    }

    static IHost BuildHost() => Host.CreateDefaultBuilder()
        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
        .UseOrleans(silo =>
        {
            silo.Services.AddTypeDiscovery();
            silo.Services.AddBindingsByConvention();
            silo.Services.AddSelfBindings();
            silo.AddChronicleToSilo(chronicle => chronicle.WithMongoDB("mongodb://localhost:27017"));
            silo.Services.AddSingleton(Substitute.For<IJobTypes>());
            silo.Services.AddSingleton(Substitute.For<ICustomSerializers>());
        }).Build();

    void Destroy()
    {
        _first?.Dispose();
        _second?.Dispose();
    }
}
