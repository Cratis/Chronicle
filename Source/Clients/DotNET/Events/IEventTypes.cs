// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Defines a system to work with <see cref="EventType">event types</see>.
/// </summary>
public interface IEventTypes
{
    /// <summary>
    /// Get all event types as <see cref="EventTypeRegistration"/>.
    /// </summary>
    IImmutableList<Type> AllClrTypes { get; }

    /// <summary>
    /// Discover all event types from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all event types with the Cratis Kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Check if there is a registered <see cref="Type">Clr Type</see> for a specific <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to check for.</param>
    /// <returns>True if there is, false if not.</returns>
    bool HasFor(EventTypeId eventTypeId);

    /// <summary>
    /// Get a <see cref="Type">Clr Type</see> for a specific <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <returns>The <see cref="Type">Clr Type</see>.</returns>
    Type GetClrTypeFor(EventTypeId eventTypeId);

    /// <summary>
    /// Check if there is a registered <see cref="EventTypeId"/> for a specific <see cref="Type">Clr Type</see>.
    /// </summary>
    /// <param name="clrType"><see cref="Type">Clr Type</see> to check for.</param>
    /// <returns>True if there is, false if not.</returns>
    bool HasFor(Type clrType);

    /// <summary>
    /// Get a <see cref="EventType"/> for a specific <see cref="Type">Clr Type</see>.
    /// </summary>
    /// <param name="clrType"><see cref="Type">Clr Type</see> to get for.</param>
    /// <returns>The <see cref="EventType"/>.</returns>
    EventType GetEventTypeFor(Type clrType);
}
