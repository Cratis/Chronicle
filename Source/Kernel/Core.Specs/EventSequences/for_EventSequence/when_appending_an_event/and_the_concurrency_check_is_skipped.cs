// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// The other direction of the same report. A scope that narrows the append but names no expectation is skipped, and
/// the append succeeds anyway - which is precisely the outcome that used to be indistinguishable from a check having
/// passed.
/// </summary>
public class and_the_concurrency_check_is_skipped : given.an_event_sequence
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        new ConcurrencyScope(EventSequenceNumber.Unavailable, false, null, null, new EventSourceType("Thing"), null));

    [Fact] void should_report_that_the_concurrency_check_was_not_performed() => _result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] void should_still_append_the_event() => _result.IsSuccess.ShouldBeTrue();
}
