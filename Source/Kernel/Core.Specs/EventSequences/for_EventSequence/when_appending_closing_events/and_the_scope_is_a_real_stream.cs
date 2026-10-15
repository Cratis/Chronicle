// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events;

public class and_the_scope_is_a_real_stream : given.a_stream_only_closing_constraint
{
    AppendResult _result;
    bool _closed;

    async Task Because()
    {
        _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, "accounting", "period", _eventType, new(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        _closed = (await _closures.GetAll(new(EventStreamType: "accounting", EventStreamId: "period"))).Any();
    }

    [Fact] void should_accept_the_closing_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_close_the_real_stream() => _closed.ShouldBeTrue();
}
