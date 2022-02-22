// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a provider of events for <see cref="IProjection">projections</see>.
    /// </summary>
    public interface IProjectionEventProvider
    {
        /// <summary>
        /// Gets the unique type identifier.
        /// </summary>
        ProjectionEventProviderTypeId TypeId { get; }

        /// <summary>
        /// Start providing events for a <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> to start providing for.</param>
        /// <param name="subject"><see cref="ISubject{Event}"/> to provide into.</param>
        /// <returns>Awaitable task.</returns>
        /// <remarks>
        /// The provider will provide events from the current position it has recorded for the
        /// <see cref="IProjection"/>. It will be in a state of catching up till its at the
        /// head of the stream. Once at the head, it will provide events as they occur.
        /// </remarks>
        Task ProvideFor(IProjectionPipeline pipeline, ISubject<AppendedEvent> subject);

        /// <summary>
        /// Stop providing for a specific <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> to stop for.</param>
        /// <returns>Awaitable task.</returns>
        Task StopProvidingFor(IProjectionPipeline pipeline);

        /// <summary>
        /// Get the last instance of a specific event type for an event source.
        /// </summary>
        /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
        /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
        /// <returns>The <see cref="AppendedEvent"/> found.</returns>
        Task<AppendedEvent> GetLastInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId);

        /// <summary>
        /// Get events from a specific sequence numbers.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> to start get for.</param>
        /// <param name="sequenceNumber">The start number to get from - inclusive.</param>
        /// <returns><see cref="IEventCursor"/>.</returns>
        Task<IEventCursor> GetFromSequenceNumber(IProjection projection, EventSequenceNumber sequenceNumber);
    }
}
