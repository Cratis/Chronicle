// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_measuring_allocations_of_an_ordinary_append : given.an_event_sequence
{
    AppendResult _result;
    protected override EventSequenceId SequenceId => "orders";

    async Task Because() => await AllocationProbe.Measure("ordinary-append", 300, async () => _result = await AppendAnEvent());

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
}
