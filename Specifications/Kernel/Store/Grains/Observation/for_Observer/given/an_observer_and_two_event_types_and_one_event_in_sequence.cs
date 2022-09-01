// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.given;

public class an_observer_and_two_event_types_and_one_event_in_sequence : an_observer_and_two_event_types
{
    protected EventSourceId event_source_id;
    protected AppendedEvent appended_event;

    void Establish()
    {
        event_source_id = Guid.NewGuid().ToString();

        appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(event_source_id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult(EventSequenceNumber.First));
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, event_source_id)).Returns(Task.FromResult(EventSequenceNumber.First));
        storage.Invocations.Clear();
    }
}
