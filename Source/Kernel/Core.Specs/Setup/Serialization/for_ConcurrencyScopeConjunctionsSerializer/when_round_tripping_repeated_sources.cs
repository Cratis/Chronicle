// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_ConcurrencyScopeConjunctionsSerializer;

public class when_round_tripping_repeated_sources : Specification
{
    ConcurrencyScopeConjunctions _result;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddCratisOrleansSerializers();
        services.AddSingleton(Substitute.For<Cratis.Chronicle.Json.IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<Storage.IStorage>());
        services.AddCustomSerializers();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        var original = new ConcurrencyScopeConjunctions([("source", new(5, true, "stream", "first", null, null), 0), ("source", new(6, true, "stream", "second", null, null), 2)]);
        _result = serializer.Deserialize<ConcurrencyScopeConjunctions>(serializer.SerializeToArray(original));
    }

    [Fact] void should_keep_both_guards() => _result.Entries.Count.ShouldEqual(2);
    [Fact] void should_keep_the_second_stream() => _result.Entries[1].Scope.EventStreamId!.Value.ShouldEqual("second");
    [Fact] void should_keep_original_positions() => _result.Entries[1].ScopeIndex.ShouldEqual((uint?)2);
}
