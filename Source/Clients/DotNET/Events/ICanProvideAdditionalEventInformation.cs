// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Defines something that can provide metadata to all events being appended.
/// </summary>
public interface ICanProvideAdditionalEventInformation
{
    /// <summary>
    /// Provide additional information on events being appended.
    /// </summary>
    /// <param name="event">The event being appended.</param>
    /// <returns>Awaitable task.</returns>
    Task ProvideFor(JsonObject @event);
}
