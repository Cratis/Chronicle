// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Events.Projections.Pipelines;

namespace Cratis.Events.Projections.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the default Cratis event log.
    /// </summary>
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        /// <inheritdoc/>
        public ProjectionEventProviderTypeId TypeId => "c0c0196f-57e3-4860-9e3b-9823cf45df30";

        /// <inheritdoc/>
        public Task<IEventCursor> GetFromPosition(IProjection projection, EventLogSequenceNumber start) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void ProvideFor(IProjectionPipeline pipeline, ISubject<Event> subject) => throw new NotImplementedException();
    }
}
