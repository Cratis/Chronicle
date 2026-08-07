// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

/// <summary>
/// Declared control - green on both sides of the append-result fix. It pins the lower bound of the input
/// space: an empty seed set must reach neither the event sequence nor the state, so the failure handling
/// added around the append has nothing to run against.
/// </summary>
public class when_seeding_no_events : given.an_event_seeding_grain
{
    async Task Because() => await _grain.Seed([]);

    [Fact]
    void should_not_append_anything() => _eventSequence.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventToAppend>>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_not_write_state() => _state.DidNotReceive().WriteStateAsync();
}
