// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_name_is_reserved_and_the_sequence_already_exists : given.a_reserved_name_check
{
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([new EventSequenceId("observers")]));
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_be_allowed() => _result.ShouldBeTrue();
}
