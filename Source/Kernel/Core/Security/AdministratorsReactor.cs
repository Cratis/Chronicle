// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security;

#pragma warning disable IDE0060 // Event handlers receive the event and its context.

/// <summary>
/// Projects separately stored administrator privileges without replacing user documents.
/// </summary>
/// <param name="storage">The system storage.</param>
public class AdministratorsReactor(IStorage storage) : Reactor
{
    /// <summary>
    /// Projects a privilege grant.
    /// </summary>
    /// <param name="event">The grant.</param>
    /// <returns>Awaitable task.</returns>
    /// <param name="context">The event context.</param>
    public Task Granted(UserGrantedAdministrator @event, EventContext context) =>
        storage.System.Administrators.Save(new Administrator(@event.UserId, @event.Reason, @event.Evidence));

    /// <summary>
    /// Projects a privilege revocation.
    /// </summary>
    /// <param name="event">The revocation.</param>
    /// <returns>Awaitable task.</returns>
    /// <param name="context">The event context.</param>
    public Task Revoked(UserRevokedAdministrator @event, EventContext context) => storage.System.Administrators.Delete(@event.UserId);

    /// <summary>
    /// Treats removal by an older node as an implicit revocation.
    /// </summary>
    /// <param name="event">The removal.</param>
    /// <param name="context">The user event context.</param>
    /// <returns>Awaitable task.</returns>
    public Task Removed(UserRemoved @event, EventContext context) =>
        storage.System.Administrators.Delete(context.EventSourceId);
}
