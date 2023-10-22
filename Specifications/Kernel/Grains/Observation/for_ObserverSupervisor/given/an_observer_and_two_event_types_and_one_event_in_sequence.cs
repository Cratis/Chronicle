// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.given;

public class an_observer_and_two_event_types_and_one_event_in_sequence : an_observer_and_two_event_types
{
    protected EventSourceId event_source_id;
    protected AppendedEvent appended_event;

    void Establish()
    {
        event_source_id = Guid.NewGuid().ToString();

        appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(event_source_id, EventSequenceNumber.First, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());

        event_sequence.Setup(_ => _.GetTailSequenceNumber()).Returns(Task.FromResult(EventSequenceNumber.First));
        event_sequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(event_types)).Returns(Task.FromResult(EventSequenceNumber.First));
        state.NextEventSequenceNumberForEventTypes = EventSequenceNumber.First;
        persistent_state.Invocations.Clear();
    }
}
