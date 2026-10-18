// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_ConcurrencyScopesSerializer;

public class when_round_tripping_legacy_scopes : Specification
{
    ConcurrencyScopes _result;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton(Substitute.For<Cratis.Chronicle.Json.IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<Storage.IStorage>());
        services.AddCustomSerializers();
        services.AddCratisOrleansSerializers();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        var original = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope> { ["source"] = new(5, true, null, "first", null, null) });
        _result = serializer.Deserialize<ConcurrencyScopes>(serializer.SerializeToArray(original));
    }

    [Fact] void should_keep_the_scope() => _result.GetFor("source").EventStreamId!.Value.ShouldEqual("first");
    [Fact] void should_keep_the_expectation() => _result.GetFor("source").SequenceNumber.Value.ShouldEqual(5UL);
}
