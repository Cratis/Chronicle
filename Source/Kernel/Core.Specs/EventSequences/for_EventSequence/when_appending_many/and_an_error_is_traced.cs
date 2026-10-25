// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

/// <summary>A converted batch exception retains its exception type on the span.</summary>
public class and_an_error_is_traced : given.an_event_sequence
{
    ActivityListener _listener;
    ActivityTraceId _trace;
    Activity _append;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == _trace && activity.OperationName == "cratis.chronicle.event_sequence.append_many") _append = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _identityStorage.GetFor(Arg.Any<Identity>()).ThrowsAsync(new InvalidOperationException("private message"));
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _eventSequence.AppendMany([new EventToAppend(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], new JsonObject())], CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    [Fact] void should_mark_the_span_as_failed() => _append.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_preserve_the_exception_type() => _append.GetTagItem("error.type").ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_record_the_message() => string.IsNullOrEmpty(_append.StatusDescription).ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}
