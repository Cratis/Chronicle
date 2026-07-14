// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using TestApp;

namespace AspNetCore;

/// <summary>
/// Reacts to employee lifecycle events and records every invocation in the instance-local
/// <see cref="ReactorInvocationLog"/>, so the web page can show which application instance the
/// kernel fanned each partition out to.
/// </summary>
/// <param name="log">The <see cref="ReactorInvocationLog"/> to record invocations in.</param>
public class HrNotificationReactor(ReactorInvocationLog log) : IReactor
{
    /// <summary>
    /// Handles the <see cref="EmployeeHired"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeHired(EmployeeHired @event, EventContext context)
    {
        log.Record(context.EventSourceId, nameof(EmployeeHired), $"{@event.FirstName} {@event.LastName} hired as {@event.Title}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeePromoted"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeePromoted(EmployeePromoted @event, EventContext context)
    {
        log.Record(context.EventSourceId, nameof(EmployeePromoted), $"Promoted to {@event.NewTitle}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeEmailSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeEmailSet(EmployeeEmailSet @event, EventContext context)
    {
        log.Record(context.EventSourceId, nameof(EmployeeEmailSet), $"Email set to {@event.Email}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeAddressSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeAddressSet(EmployeeAddressSet @event, EventContext context)
    {
        log.Record(context.EventSourceId, nameof(EmployeeAddressSet), $"Address set to {@event.City}, {@event.Country}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeMoved"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeMoved(EmployeeMoved @event, EventContext context)
    {
        log.Record(context.EventSourceId, nameof(EmployeeMoved), $"Relocated to {@event.City}, {@event.Country}");
        return Task.CompletedTask;
    }
}
