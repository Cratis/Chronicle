// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;

namespace TestApp;

[EventType]
public record EmployeeHired(string FirstName, string LastName, string Title);

[EventType]
public record EmployeePromoted(string NewTitle);

[EventType]
public record EmployeeAddressSet(string Address, string City, string ZipCode, string Country);

[EventType]
public record EmployeeMoved(string Address, string City, string ZipCode, string Country);

[EventType]
public record EmployeeEmailSet(string Email);

[FromEvent<EmployeeHired>]
[FromEvent<EmployeePromoted>]
[FromEvent<EmployeeAddressSet>]
[FromEvent<EmployeeMoved>]
[FromEvent<EmployeeEmailSet>]
public record EmployeeState(
    string FirstName = "",
    string LastName = "",
    [property: SetFrom<EmployeePromoted>(nameof(EmployeePromoted.NewTitle))]
    string Title = "",
    string Email = "",
    string Address = "",
    string City = "",
    string ZipCode = "",
    string Country = "");

// ---------------------------------------------------------------------------
// Reducer — used to back the EmployeeState read model via IReducerFor<T>
// ---------------------------------------------------------------------------

/// <summary>
/// Folds employee lifecycle events into the <see cref="EmployeeState"/> read model.
/// </summary>
public class EmployeeStateReducer : IReducerFor<EmployeeState>
{
    /// <summary>
    /// Handles an <see cref="EmployeeHired"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">Unused — employee is always new on hire.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The new <see cref="EmployeeState"/>.</returns>
    public Task<EmployeeState?> EmployeeHired(EmployeeHired @event, EmployeeState? current, EventContext context) =>
        Task.FromResult<EmployeeState?>(new EmployeeState(
            FirstName: @event.FirstName,
            LastName: @event.LastName,
            Title: @event.Title));

    /// <summary>
    /// Handles an <see cref="EmployeeAddressSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated <see cref="EmployeeState"/>.</returns>
    public Task<EmployeeState?> EmployeeAddressSet(EmployeeAddressSet @event, EmployeeState? current, EventContext context) =>
        Task.FromResult<EmployeeState?>((current ?? new EmployeeState()) with
        {
            Address = @event.Address,
            City = @event.City,
            ZipCode = @event.ZipCode,
            Country = @event.Country
        });

    /// <summary>
    /// Handles an <see cref="EmployeeEmailSet"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated <see cref="EmployeeState"/>.</returns>
    public Task<EmployeeState?> EmployeeEmailSet(EmployeeEmailSet @event, EmployeeState? current, EventContext context) =>
        Task.FromResult<EmployeeState?>((current ?? new EmployeeState()) with { Email = @event.Email });

    /// <summary>
    /// Handles an <see cref="EmployeePromoted"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated <see cref="EmployeeState"/>.</returns>
    public Task<EmployeeState?> EmployeePromoted(EmployeePromoted @event, EmployeeState? current, EventContext context) =>
        Task.FromResult<EmployeeState?>((current ?? new EmployeeState()) with { Title = @event.NewTitle });

    /// <summary>
    /// Handles an <see cref="EmployeeMoved"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated <see cref="EmployeeState"/>.</returns>
    public Task<EmployeeState?> EmployeeMoved(EmployeeMoved @event, EmployeeState? current, EventContext context) =>
        Task.FromResult<EmployeeState?>((current ?? new EmployeeState()) with
        {
            Address = @event.Address,
            City = @event.City,
            ZipCode = @event.ZipCode,
            Country = @event.Country
        });
}
