// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security;

#pragma warning disable IDE0060 // Event handlers receive the event and its context.

/// <summary>
/// Projects separately stored administrator privileges without replacing user documents.
/// </summary>
/// <param name="storage">The system storage.</param>
/// <param name="serializer">The event serializer.</param>
[Reactor(systemEventStoreOnly: true, defaultNamespaceOnly: true)]
public class AdministratorsReactor(IStorage storage, IEventSerializer serializer) : Reactor
{
    readonly AdministratorProjectionState _state = new(storage, serializer);

    /// <summary>
    /// Reconciles a privilege grant with the authoritative roster.
    /// </summary>
    /// <param name="event">The grant.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    public Task Granted(UserGrantedAdministrator @event, EventContext context) =>
        AdministratorEventScope.IsRosterEvent(context) ? Reconcile(@event.UserId) : Task.CompletedTask;

    /// <summary>
    /// Reconciles a privilege revocation with the authoritative roster.
    /// </summary>
    /// <param name="event">The revocation.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    public Task Revoked(UserRevokedAdministrator @event, EventContext context) =>
        AdministratorEventScope.IsRosterEvent(context) ? Reconcile(@event.UserId) : Task.CompletedTask;

    /// <summary>
    /// Treats removal on a system user stream by an older node as an implicit revocation.
    /// </summary>
    /// <param name="event">The removal.</param>
    /// <param name="context">The user event context.</param>
    /// <returns>Awaitable task.</returns>
    public Task Removed(UserRemoved @event, EventContext context) =>
        AdministratorEventScope.IsSystemEvent(context) && Guid.TryParse(context.EventSourceId.Value, out var userId)
            ? Reconcile(userId)
            : Task.CompletedTask;

    async Task Reconcile(UserId userId)
    {
        var administrator = await _state.Get(userId);
        while (true)
        {
            if (administrator is null)
            {
                await storage.System.Administrators.Delete(userId);
            }
            else
            {
                await storage.System.Administrators.Save(administrator);
            }

            // User removals and roster events are delivered on different partitions and can race this write.
            var current = await _state.Get(userId);
            if (current == administrator) return;
            administrator = current;
        }
    }
}
