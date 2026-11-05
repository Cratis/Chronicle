// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events;

public class and_a_closing_scope_is_too_long : given.a_stream_only_closing_constraint
{
    AppendResult _refused;
    AppendResult _ordinary;

    async Task Because()
    {
        _refused = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, "accounting", new string('p', 256), _eventType, new(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        _ordinary = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, "accounting", "period", _eventType, new(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
    }

    [Fact] void should_refuse_the_closing_event() => _refused.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_not_report_the_index_as_unavailable() => _refused.Errors.ShouldBeEmpty();
    [Fact] void should_accept_a_closing_event_that_fits() => _ordinary.IsSuccess.ShouldBeTrue();
}
