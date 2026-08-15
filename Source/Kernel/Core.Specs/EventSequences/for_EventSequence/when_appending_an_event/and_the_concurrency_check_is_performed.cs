// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// A passing concurrency check and a skipped one produce the same successful append, so the outcome has to say which
/// happened. Without it a caller that believes its writes are serialized has no way to find out otherwise short of
/// reading the server log.
/// </summary>
public class and_the_concurrency_check_is_performed : given.an_event_sequence
{
    AppendResult _result;

    void Establish() =>
        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(EventSequenceNumber.Unavailable);

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
        new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, new EventSourceType("Customer"), null));

    [Fact] void should_report_that_the_concurrency_check_was_performed() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_append_the_event() => _result.IsSuccess.ShouldBeTrue();
}
