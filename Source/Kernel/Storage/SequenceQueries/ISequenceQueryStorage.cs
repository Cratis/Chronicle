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
    /// Get a single query by its identifier, regardless of who owns it.
    /// </summary>
    /// <param name="id">The <see cref="SequenceQueryId"/> of the query to get.</param>
    /// <returns>The <see cref="SequenceQueryDefinition"/> if it exists, otherwise null.</returns>
    /// <remarks>
    /// Intended for deciding whether a change is permitted, so it does not narrow by owner.
    /// </remarks>
    Task<SequenceQueryDefinition?> Get(SequenceQueryId id);

    /// <summary>
    /// Save a query that is not stored yet. An existing query with the same identifier is never replaced.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryDefinition"/> to save.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when a query with the identifier is already stored, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/>.</returns>
    Task<SequenceQueryWriteOutcome> Insert(SequenceQueryDefinition definition);

    /// <summary>
    /// Replace a stored query, but only while it is still stored under the expected owner.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryDefinition"/> to save. The owner it is stored under is kept.</param>
    /// <param name="expectedOwner">The <see cref="SequenceQueryOwner"/> the query was checked to be stored under, compared exactly.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when no query with the identifier is stored under the expected owner, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/>.</returns>
    Task<SequenceQueryWriteOutcome> Update(SequenceQueryDefinition definition, SequenceQueryOwner expectedOwner);

    /// <summary>
    /// Delete a query, but only while it is still stored under the expected owner.
    /// </summary>
    /// <param name="id">The <see cref="SequenceQueryId"/> of the query to delete.</param>
    /// <param name="expectedOwner">The <see cref="SequenceQueryOwner"/> the query was checked to be stored under, compared exactly.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when the query is still stored under another owner, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/> - including when it is already gone.</returns>
    Task<SequenceQueryWriteOutcome> Delete(SequenceQueryId id, SequenceQueryOwner expectedOwner);

    /// <summary>
    /// Get every folder visible to an owner - the ones they created, plus the ones shared with everyone.
    /// </summary>
    /// <param name="owner">The <see cref="SequenceQueryOwner"/> to get for.</param>
    /// <returns>A collection of <see cref="SequenceQueryFolderDefinition"/>.</returns>
    Task<IEnumerable<SequenceQueryFolderDefinition>> GetAllFoldersFor(SequenceQueryOwner owner);

    /// <summary>
    /// Get a single folder by its identifier, regardless of who owns it.
    /// </summary>
    /// <param name="id">The <see cref="SequenceQueryFolderId"/> of the folder to get.</param>
    /// <returns>The <see cref="SequenceQueryFolderDefinition"/> if it exists, otherwise null.</returns>
    /// <remarks>
    /// Intended for deciding whether a change is permitted, so it does not narrow by owner.
    /// </remarks>
    Task<SequenceQueryFolderDefinition?> GetFolder(SequenceQueryFolderId id);

    /// <summary>
    /// Save a folder that is not stored yet. An existing folder with the same identifier is never replaced.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryFolderDefinition"/> to save.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when a folder with the identifier is already stored, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/>.</returns>
    Task<SequenceQueryWriteOutcome> InsertFolder(SequenceQueryFolderDefinition definition);

    /// <summary>
    /// Replace a stored folder, but only while it is still stored under the expected owner.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryFolderDefinition"/> to save. The owner it is stored under is kept.</param>
    /// <param name="expectedOwner">The <see cref="SequenceQueryOwner"/> the folder was checked to be stored under, compared exactly.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when no folder with the identifier is stored under the expected owner, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/>.</returns>
    Task<SequenceQueryWriteOutcome> UpdateFolder(SequenceQueryFolderDefinition definition, SequenceQueryOwner expectedOwner);

    /// <summary>
    /// Delete a folder, but only while it is still stored under the expected owner.
    /// </summary>
    /// <param name="id">The <see cref="SequenceQueryFolderId"/> of the folder to delete.</param>
    /// <param name="expectedOwner">The <see cref="SequenceQueryOwner"/> the folder was checked to be stored under, compared exactly.</param>
    /// <returns><see cref="SequenceQueryWriteOutcome.Conflict"/> when the folder is still stored under another owner, otherwise <see cref="SequenceQueryWriteOutcome.Applied"/> - including when it is already gone.</returns>
    /// <remarks>
    /// Only the folder itself is removed. What is filed underneath it - queries and nested folders
    /// alike - is the caller's to deal with, because whether they move or go with it is a decision
    /// the storage has no business making.
    /// </remarks>
    Task<SequenceQueryWriteOutcome> DeleteFolder(SequenceQueryFolderId id, SequenceQueryOwner expectedOwner);
}
