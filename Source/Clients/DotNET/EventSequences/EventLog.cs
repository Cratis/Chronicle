// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventLog"/> class.
/// </remarks>
/// <param name="eventStoreName">Name of the event store.</param>
/// <param name="namespace"><see cref="EventStoreNamespaceName"/> the sequence is for.</param>
/// <param name="connection"><see cref="IChronicleConnection"/> for getting connections.</param>
/// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
/// <param name="constraints">Known <see cref="IConstraints"/>.</param>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting correlation.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
public class EventLog(
     EventStoreName eventStoreName,
     EventStoreNamespaceName @namespace,
     IChronicleConnection connection,
     IEventTypes eventTypes,
     IConstraints constraints,
     IEventSerializer eventSerializer,
     ICorrelationIdAccessor correlationIdAccessor,
     ICausationManager causationManager,
     IIdentityProvider identityProvider) : EventSequence(
        eventStoreName,
        @namespace,
        EventSequenceId.Log,
        connection,
        eventTypes,
        constraints,
        eventSerializer,
        correlationIdAccessor,
        causationManager,
        identityProvider), IEventLog
{
}
