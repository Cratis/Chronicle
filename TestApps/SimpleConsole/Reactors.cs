// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace TestApp;

/// <summary>
/// Reacts to employee lifecycle events by emitting console notifications.
/// </summary>
public class HrNotificationReactor : IReactor
{
    /// <summary>
    /// Handles the <see cref="EmployeeHired"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeHired(EmployeeHired @event)
    {
        Console.WriteLine($"[REACTOR] Employee hired: {@event.FirstName} {@event.LastName} as {@event.Title}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeePromoted"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeePromoted(EmployeePromoted @event)
    {
        Console.WriteLine($"[REACTOR] Employee promoted to {@event.NewTitle}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeEmailSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeEmailSet(EmployeeEmailSet @event)
    {
        Console.WriteLine($"[REACTOR] Employee email set to {@event.Email}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeAddressSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeAddressSet(EmployeeAddressSet @event)
    {
        Console.WriteLine($"[REACTOR] Employee address set to {@event.City}, {@event.Country}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="EmployeeMoved"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>A completed task.</returns>
    public Task EmployeeMoved(EmployeeMoved @event)
    {
        Console.WriteLine($"[REACTOR] Employee relocated to {@event.City}, {@event.Country}");
        return Task.CompletedTask;
    }
}
