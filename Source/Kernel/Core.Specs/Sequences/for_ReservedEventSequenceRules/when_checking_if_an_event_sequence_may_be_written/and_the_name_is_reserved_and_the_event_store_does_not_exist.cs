// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_name_is_reserved_and_the_event_store_does_not_exist : given.a_reserved_name_check
{
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(false);
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_be_refused() => _result.ShouldBeFalse();
    [Fact] void should_not_create_the_event_store() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
}
