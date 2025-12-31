// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IProjection : IGrainWithStringKey
{
    /// <summary>
    /// Set the projection definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(ProjectionDefinition definition);

    /// <summary>
    /// Get the projection definition.
    /// </summary>
    /// <returns>The current <see cref="ProjectionDefinition"/>.</returns>
    Task<ProjectionDefinition> GetDefinition();

    /// <summary>
    /// Subscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Unsubscribe to changes in projection or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyProjectionDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber);

    /// <summary>
    /// Get the event types the projection is interested in.
    /// </summary>
    /// <returns>The event types.</returns>
    Task<IEnumerable<EventType>> GetEventTypes();

    /// <summary>
    /// Process a set of events through the projection for a single read-model instance.
    /// </summary>
    /// <param name="eventStoreNamespace">The namespace the events are from.</param>
    /// <param name="initialState">The initial projected state.</param>
    /// <param name="events">The events to process.</param>
    /// <returns>The resulting projected state.</returns>
    Task<ExpandoObject> ProcessForSingleReadModel(EventStoreNamespaceName eventStoreNamespace, ExpandoObject initialState, IEnumerable<AppendedEvent> events);

    /// <summary>
    /// Process a set of events through the projection and return resulting read-model instances grouped by key.
    /// </summary>
    /// <param name="eventStoreNamespace">The namespace the events are from.</param>
    /// <param name="events">The events to process.</param>
    /// <returns>The resulting projected states for all keys encountered.</returns>
    Task<IEnumerable<ExpandoObject>> Process(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events);
}
