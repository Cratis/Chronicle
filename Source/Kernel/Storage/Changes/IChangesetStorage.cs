// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Defines the storage mechanism for changesets. Typically used for debugging purposes to see what changes has occurred.
/// </summary>
public interface IChangesetStorage
{
    /// <summary>
    /// Save changesets associated with a specific <see cref="CorrelationId"/>.
    /// </summary>
    /// <param name="projectionIdentifier">The <see cref="ProjectionId"/>.</param>
    /// <param name="projectionObjectKey">The <see cref="Key"/>.</param>
    /// <param name="projectionPath">The <see cref="ProjectionPath"/>.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to save for.</param>
    /// <param name="changeset">All the associated <see cref="IChangeset{Event, ExpandoObject}">changesets</see>.</param>
    /// <returns>Async task.</returns>
    Task Save(
        ProjectionId projectionIdentifier,
        Key projectionObjectKey,
        ProjectionPath projectionPath,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset);
}
