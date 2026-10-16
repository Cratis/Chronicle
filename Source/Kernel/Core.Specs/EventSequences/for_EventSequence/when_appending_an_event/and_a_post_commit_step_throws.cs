// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_post_commit_step_throws : given.an_append_with_settlement
{
    void Establish() => _appendedEventsQueues.Enqueue(Arg.Any<IEnumerable<AppendedEvent>>()).ThrowsAsync(new given.SimulatedStorageError());
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_spill_delivery() => await _appendedEventsQueues.Received(1).SpillToCatchup();
}
