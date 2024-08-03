// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Defines the constraints system for an event store.
/// </summary>
public interface IConstraints : IGrainWithStringKey
{
    /// <summary>
    /// Check if an event can be applied to an event source.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to check for.</param>
    /// <param name="eventType">The <see cref="EventType"/> to check for.</param>
    /// <param name="content">The content of the event.</param>
    /// <returns><see cref="ConstraintCheckResult"/> with the result.</returns>
    Task<ConstraintCheckResult> Check(EventSourceId eventSourceId, EventType eventType, JsonObject content);
}
