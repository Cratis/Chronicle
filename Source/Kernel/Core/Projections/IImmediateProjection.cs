// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines an projection.
/// </summary>
/// <remarks>
/// The compound identity is based on the actual event source id.
/// This ensures that we can run multiple of these in a cluster for a specific type without
/// having to wait for turn if its not the same identifier.
/// </remarks>
public interface IImmediateProjection : IGrainWithStringKey
{
    /// <summary>
    /// Get the model instance.
    /// </summary>
    /// <returns>The <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetModelInstance();

    /// <summary>
    /// Initializes a fresh, isolated decision read with the definition validated by the caller.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <param name="eventTypes">The admitted projection's event types for this namespace, including removals.</param>
    /// <returns>Awaitable task.</returns>
    Task InitializeForDecision(Concepts.Projections.Definitions.ProjectionDefinition definition, IEnumerable<Concepts.Events.EventType> eventTypes);

    /// <summary>
    /// Get the current model instance with additional events applied. Ignoring any new events from the event store.
    /// </summary>
    /// <param name="events">Collection of events to apply.</param>
    /// <returns>The <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetCurrentModelInstanceWithAdditionalEventsApplied(IEnumerable<EventToApply> events);

    /// <summary>
    /// Dehydrate the projection instance.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Dehydrate();
}
