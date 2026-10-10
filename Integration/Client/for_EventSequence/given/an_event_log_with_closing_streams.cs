// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

public class an_event_log_with_closing_streams(ChronicleFixture fixture) : Specification(fixture)
{
    public override IEnumerable<Type> EventTypes => [typeof(StreamActivity), typeof(StreamClosed), typeof(StreamReopened)];
    public override IEnumerable<Type> ConstraintTypes => [typeof(ClosingStream)];

    public EventSourceId Source { get; } = Guid.NewGuid().ToString();
    public EventStreamType StreamType { get; } = "accounting";
    public EventStreamId StreamId { get; } = "period";
    public ClosedStreamScope Scope => new(Source, EventStreamType: StreamType, EventStreamId: StreamId);

    public async Task<IAppendResult> Append(object @event) => await EventStore.EventLog.Append(Source, @event, eventStreamType: StreamType, eventStreamId: StreamId);
    public async Task<IAppendResult> AppendMany(params object[] events) => await EventStore.EventLog.AppendMany(Source, events, eventStreamType: StreamType, eventStreamId: StreamId);
}
