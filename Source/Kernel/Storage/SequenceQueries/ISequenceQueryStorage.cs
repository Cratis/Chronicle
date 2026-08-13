// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.SequenceQueries;

/// <summary>
/// Defines a system for working with saved <see cref="SequenceQueryDefinition">event sequence queries</see>.
/// </summary>
/// <remarks>
/// Saved queries are workbench state, not domain facts - they are edited in place as the user changes
/// filters, so they are stored directly rather than derived from events.
/// </remarks>
public interface ISequenceQueryStorage
{
    /// <summary>
    /// Get every query visible to an owner - the ones they saved, plus the ones shared with everyone.
    /// </summary>
    /// <param name="owner">The <see cref="SequenceQueryOwner"/> to get for.</param>
    /// <returns>A collection of <see cref="SequenceQueryDefinition"/>.</returns>
    Task<IEnumerable<SequenceQueryDefinition>> GetAllFor(SequenceQueryOwner owner);

    /// <summary>
    /// Observe every query visible to an owner - the ones they saved, plus the ones shared with everyone.
    /// </summary>
    /// <param name="owner">The <see cref="SequenceQueryOwner"/> to observe for.</param>
    /// <returns>A <see cref="ISubject{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="SequenceQueryDefinition"/>.</returns>
    ISubject<IEnumerable<SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner);

    /// <summary>
    /// Save a query, replacing any existing query with the same identifier.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryDefinition"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(SequenceQueryDefinition definition);

    /// <summary>
    /// Delete a query.
    /// </summary>
    /// <param name="id">The <see cref="SequenceQueryId"/> of the query to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(SequenceQueryId id);
}
