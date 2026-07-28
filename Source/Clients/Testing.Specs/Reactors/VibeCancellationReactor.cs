// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor that, on a <see cref="VibeCancelled"/> event, reads the <see cref="VibeAttendees"/> read model
/// (a handler-method parameter), audits via a service method parameter, and notifies via a constructor dependency —
/// exercising every argument-resolution path of <see cref="ReactorScenario{TReactor}"/>.
/// </summary>
/// <param name="notifications">The notification service (constructor dependency).</param>
/// <param name="logger">The logger (resolved from the scenario's default logging registration).</param>
public class VibeCancellationReactor(INotificationService notifications, ILogger<VibeCancellationReactor> logger) : IReactor
{
    /// <summary>
    /// Reacts to a <see cref="VibeCancelled"/> event by auditing and notifying the host of the vibe.
    /// </summary>
    /// <param name="event">The triggering <see cref="VibeCancelled"/> event.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <param name="attendees">The <see cref="VibeAttendees"/> read model, materialized for the vibe.</param>
    /// <param name="audit">The <see cref="IVibeAudit"/> service resolved as a method parameter.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task VibeCancelled(VibeCancelled @event, EventContext context, VibeAttendees attendees, IVibeAudit audit)
    {
        logger.VibeCancelled(attendees.Host);
        audit.Record(attendees.Host);
        return notifications.Notify(attendees.Host);
    }
}
