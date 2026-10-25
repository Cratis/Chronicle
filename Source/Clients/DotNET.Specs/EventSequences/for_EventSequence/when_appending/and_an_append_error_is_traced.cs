// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

/// <summary>A failed append response marks the client append span.</summary>
public class and_an_append_error_is_traced : given.an_event_sequence
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
                if (activity.TraceId == _trace && activity.OperationName == "client.event_sequence.append") _append = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.System);
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.Empty, new() { Errors = ["private message"] }));
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _eventSequence.Append("source", "event");
    }

    [Fact] void should_mark_the_span_as_failed() => _append.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_error_code() => _append.GetTagItem("error.type").ShouldEqual("append_error");
    [Fact] void should_not_record_the_message() => string.IsNullOrEmpty(_append.StatusDescription).ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}
