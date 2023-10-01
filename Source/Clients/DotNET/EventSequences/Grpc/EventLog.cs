// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.EventSequences.Grpc;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/>.
/// </summary>
public class EventLog : EventSequence, IEventLog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the sequence is for.</param>
    /// <param name="connection"><see cref="ICratisConnection"/> for getting connections.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
   public EventLog(
        EventStoreName eventStoreName,
        TenantId tenantId,
        ICratisConnection connection,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        IIdentityProvider identityProvider) : base(
            eventStoreName,
            tenantId,
            EventSequenceId.Log,
            connection,
            eventTypes,
            eventSerializer,
            causationManager,
            identityProvider)
    {
    }
}
