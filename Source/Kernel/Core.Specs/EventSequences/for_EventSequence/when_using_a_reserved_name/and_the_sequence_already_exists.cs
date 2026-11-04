// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_already_exists : given.an_event_sequence_with_a_reserved_name
{
    AppendResult _result;
    protected override bool SequenceExists => true;

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_reach_storage() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
