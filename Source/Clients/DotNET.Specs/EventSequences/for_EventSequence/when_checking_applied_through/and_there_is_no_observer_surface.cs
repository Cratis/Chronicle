// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_checking_applied_through;

public class and_there_is_no_observer_surface : given.an_event_sequence
{
    Exception _exception;

    void Establish() => services.Observers.Returns(_ => throw new NotSupportedException());

    async Task Because() => _exception = await Catch.Exception(() => _eventSequence.AppliedThrough(["observer-1"], 42UL));

    [Fact] void should_fail_by_name() => _exception.ShouldBeOfExactType<CannotWaitForObserverCompletion>();
}
