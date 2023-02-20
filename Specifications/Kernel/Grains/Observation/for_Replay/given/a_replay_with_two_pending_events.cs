// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Replay.given;

public class a_replay_with_two_pending_events : a_replay_worker
{
    protected AppendedEvent first_appended_event;
    protected AppendedEvent second_appended_event;
    protected EventSourceId first_event_source_id;
    protected EventSourceId second_event_source_id;
    protected IEnumerable<EventType> event_types = new EventType[]
   {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1)
   };

    protected override IEnumerable<AppendedEvent> events => new[]
    {
        first_appended_event,
        second_appended_event
    };

    void Establish()
    {
        state.EventTypes = event_types;

        first_event_source_id = Guid.NewGuid();
        first_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(first_event_source_id, EventSequenceNumber.First, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());

        second_event_source_id = Guid.NewGuid();
        second_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First + 1, event_types.ToArray()[0]),
            new(second_event_source_id, EventSequenceNumber.First + 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());
    }

}
