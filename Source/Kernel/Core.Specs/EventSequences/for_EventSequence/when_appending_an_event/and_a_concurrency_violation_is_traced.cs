// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>Concurrency validation is included in the append span.</summary>
public class and_a_concurrency_violation_is_traced : given.an_event_sequence
{
    ActivityListener _listener;
    ActivityTraceId _trace;
    Activity _append;
    AppendResult _result;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == _trace && activity.OperationName == "cratis.chronicle.event_sequence.append") _append = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>()).Returns((EventSequenceNumber)10);
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System, [], new ConcurrencyScope(1, true, null, null, new EventSourceType("Customer"), null));
    }

    [Fact] void should_report_the_violation() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_mark_the_span_as_failed() => _append.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_failure_code() => _append.GetTagItem("error.type").ShouldEqual("concurrency_violation");

    void Destroy() => _listener.Dispose();
}
