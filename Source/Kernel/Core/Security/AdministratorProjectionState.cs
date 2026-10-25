// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Recomputes projected privileges from the authoritative system roster, not delivery order.
/// </summary>
/// <param name="storage">The system event storage.</param>
/// <param name="serializer">The event serializer.</param>
internal class AdministratorProjectionState(IStorage storage, IEventSerializer serializer)
{
    static readonly EventType[] _rosterTypes = [typeof(UserGrantedAdministrator).GetEventType(), typeof(UserRevokedAdministrator).GetEventType()];

    IEventSequenceStorage Events => storage.GetEventStore(EventStoreName.System)
        .GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <summary>
    /// Computes the administrator state for a user from the authoritative roster events in the System event store.
    /// </summary>
    /// <param name="userId">The <see cref="UserId"/> to compute the state for.</param>
    /// <returns>The <see cref="Administrator"/> if the user holds the privilege; otherwise null.</returns>
    internal async Task<Administrator?> Get(UserId userId)
    {
        var tail = await Events.GetTailSequenceNumber(_rosterTypes, Roster.EventSourceId);
        if (!tail.IsActualValue) return null;

        var administrator = await Replay(userId, tail);
        return await HasBeenRemoved(userId) ? null : administrator;
    }

    static Administrator? Apply(Administrator? current, UserId userId, object content) => content switch
    {
        UserGrantedAdministrator grant when grant.UserId == userId => new Administrator(userId, grant.Reason, grant.Evidence),
        UserRevokedAdministrator revoke when revoke.UserId == userId => null,
        _ => current
    };

    async Task<Administrator?> Replay(UserId userId, EventSequenceNumber tail)
    {
        Administrator? administrator = null;
        using var cursor = await Events.GetRange(EventSequenceNumber.First, tail, Roster.EventSourceId, _rosterTypes);
        while (await cursor.MoveNext())
        {
            administrator = cursor.Current
                .Where(@event => AdministratorEventScope.IsRosterEvent(@event.Context))
                .Aggregate(administrator, (current, @event) => Apply(current, userId, serializer.Deserialize(@event)));
        }

        return administrator;
    }

    async Task<bool> HasBeenRemoved(UserId userId)
    {
        EventType[] types = [typeof(UserRemoved).GetEventType()];
        var tail = await Events.GetTailSequenceNumber(types, userId);
        if (!tail.IsActualValue) return false;
        using var cursor = await Events.GetRange(EventSequenceNumber.First, tail, userId, types);
        while (await cursor.MoveNext())
        {
            if (cursor.Current.Any(@event => AdministratorEventScope.IsSystemEvent(@event.Context))) return true;
        }
        return false;
    }
}
