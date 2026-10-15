// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_activating_the_bootstrapped_system_sequence : given.an_event_sequence
{
    /// <summary>
    /// Defers the reusable context's activation so Because exercises the System/Default key.
    /// </summary>
    /// <returns>A completed task without activating the grain.</returns>
    protected override Task<EventSequence> CreateEventSequence() => Task.FromResult<EventSequence>(null!);

    async Task Because()
    {
        _eventSequenceKey = new EventSequenceKey(EventSequenceId.System, EventStoreName.System, EventStoreNamespaceName.Default);
        _eventSequence = await base.CreateEventSequence();
    }

    [Fact] async Task should_not_call_back_into_the_system_namespaces_grain() => await _namespaces.DidNotReceive().Ensure(EventStoreNamespaceName.Default);
}
