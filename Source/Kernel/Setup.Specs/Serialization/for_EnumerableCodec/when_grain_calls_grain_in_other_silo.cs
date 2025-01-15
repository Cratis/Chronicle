// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Applications.Orleans.Concepts;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Runtime.Placement;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace Cratis.Chronicle.Setup.Serialization.for_EnumerableCodec;


[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class SpecificSiloPlacementStrategy : PlacementStrategy
{
    internal static readonly SpecificSiloPlacementDirector Singleton = new();
}

public class SpecificSiloPlacementDirector : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var key = target.GrainIdentity.GetIntegerKey();
        var silos = context.GetCompatibleSilos(target).ToArray();
        return Task.FromResult(silos[key]);
    }
}


public class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.Configure<ServiceProviderOptions>(options =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });

        siloBuilder
            .AddPlacementDirector<SpecificSiloPlacementStrategy, SpecificSiloPlacementDirector>();

        siloBuilder.Services.AddConceptSerializer();
        siloBuilder.Services.AddAppendedEventSerializer();
        siloBuilder.Services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
            _ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Cratis") ?? false),
            new JsonSerializerOptions()));
    }
}

public interface IFirstGrain : IGrainWithIntegerKey
{
    Task CallSecond();
}

public interface ISecondGrain : IGrainWithIntegerKey
{
    Task Receiver();

    Task<string> GetReceived();
}

public class FirstGrain : Grain, IFirstGrain
{
    public Task CallSecond()
    {
        var second = GrainFactory.GetGrain<ISecondGrain>(1);
        return second.Receiver();
    }
}

public class SecondGrain : Grain, ISecondGrain
{
    public Task Receiver()
    {
        return Task.CompletedTask;
    }

    public Task<string> GetReceived() => Task.FromResult("Hello");
}


public class when_grain_calls_grain_in_other_silo : Specification
{
    IFirstGrain _firstGrain;
    ISecondGrain _secondGrain;
    string _result;

    void Establish()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        var cluster = builder.Build();
        cluster.Deploy();

        _firstGrain = cluster.GrainFactory.GetGrain<IFirstGrain>(0);
        _secondGrain = cluster.GrainFactory.GetGrain<ISecondGrain>(1);
    }

    async Task Because()
    {
        await _firstGrain.CallSecond();
        _result = await _secondGrain.GetReceived();
    }


    [Fact] void should_get_result() => _result.ShouldEqual("Hello");
}
