// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events;

public class and_the_scope_is_default_stream_only : given.a_stream_only_closing_constraint
{
    AppendResult _result;
    bool _closed;

    async Task Because()
    {
        _result = await AppendAnEvent();
        _closed = (await _closures.GetAll()).Any();
    }

    [Fact] void should_refuse_the_closing_event() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_persist_nothing() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_close_the_default_stream() => _closed.ShouldBeFalse();
}
