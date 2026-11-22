// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkEvents;

/// <summary>Verifies that fixed fixtures reject unregistered event types and generations.</summary>
public class when_resolving_unregistered_types : Specification
{
    bool _unknown;
    bool _generation;
    Exception? _unknownFailure;
    Exception? _generationFailure;

    async Task Because()
    {
        var types = BenchmarkEvents.CreateEventTypes();
        _unknown = await types.HasFor("unknown");
        _generation = await types.HasFor(BenchmarkEvents.Selected.Id, new EventTypeGeneration(2));
        _unknownFailure = await Catch.Exception(() => types.GetFor("unknown"));
        _generationFailure = await Catch.Exception(() => types.GetFor(BenchmarkEvents.Selected.Id, new EventTypeGeneration(2)));
    }

    [Fact] void should_not_claim_unknown_types() => _unknown.ShouldBeFalse();
    [Fact] void should_not_claim_unknown_generations() => _generation.ShouldBeFalse();
    [Fact] void should_reject_unknown_types() => _unknownFailure.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_reject_unknown_generations() => _generationFailure.ShouldBeOfExactType<NotSupportedException>();
}
