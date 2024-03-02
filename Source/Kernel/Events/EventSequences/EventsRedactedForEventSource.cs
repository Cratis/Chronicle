// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Events.EventSequences;

/// <summary>
/// The event that occurs when a collection of events are redacted based on their <see cref="EventSourceId"/>.
/// </summary>
/// <param name="Microservice">The <see cref="MicroserviceId"/> the event was redacted for.</param>
/// <param name="TenantId">The <see cref="Execution.TenantId"/> the event was redacted for.</param>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="EventSourceId"><see cref="EventSourceId"/> representing the events that was redacted.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> to redact.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
[EventType("54d6962c-2ef5-43c9-8251-c62426ef857e")]
public record EventsRedactedForEventSource(
    MicroserviceId Microservice,
    TenantId TenantId,
    EventSequenceId Sequence,
    EventSourceId EventSourceId,
    IEnumerable<EventType> EventTypes,
    RedactionReason Reason);
