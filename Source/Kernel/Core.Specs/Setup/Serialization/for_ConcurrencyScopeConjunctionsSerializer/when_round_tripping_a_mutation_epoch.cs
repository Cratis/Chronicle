// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_ConcurrencyScopeConjunctionsSerializer;

public class when_round_tripping_a_mutation_epoch : Specification
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
        var original = new ConcurrencyScopeConjunctions([("source", new(5, true, null, null, null, null) { MutationEpoch = new EventSequenceMutationEpoch(4) }, 0)]);
        _result = serializer.Deserialize<ConcurrencyScopeConjunctions>(serializer.SerializeToArray(original));
    }

    [Fact] void should_keep_the_mutation_epoch() => _result.Entries[0].Scope.MutationEpoch.ShouldEqual(new EventSequenceMutationEpoch(4));
}
