// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 16.45.x compatibility surface - do not remove.
//
// This service interface was the client contract up to and including 16.45.x and was replaced by
// the generated 17+ surface. Compiled consumers (e.g. Cratis.Stage 3.11.0's generated type
// bindings) hold assembly references to it and fail to load when it is absent. The [Service] and
// [Operation] attributes are deliberately stripped: the 18.x kernel does not serve this service,
// and the canonical descriptor set must not advertise it - advertising it made every 18.1.1 client
// refuse an 18.1.0 kernel at connect ("the server no longer serves 13 things this client
// expects"). The types exist for assembly-level compatibility only, not for the wire.
namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Defines the contract for working with saved event sequence queries.
/// </summary>
public interface ISequenceQueries
{
    /// <summary>
    /// Get the saved queries an owner can see.
    /// </summary>
    /// <param name="request">The <see cref="GetSequenceQueriesRequest"/>.</param>
    /// <returns>A collection of <see cref="SequenceQueryDefinition"/>.</returns>
    Task<IEnumerable<SequenceQueryDefinition>> GetSequenceQueries(GetSequenceQueriesRequest request);

    /// <summary>
    /// Observe the saved queries an owner can see.
    /// </summary>
    /// <param name="request">The <see cref="GetSequenceQueriesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An <see cref="IObservable{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="SequenceQueryDefinition"/>.</returns>
    IObservable<IEnumerable<SequenceQueryDefinition>> ObserveSequenceQueries(GetSequenceQueriesRequest request, CallContext context = default);

    /// <summary>
    /// Save a query, replacing any existing query with the same identifier.
    /// </summary>
    /// <param name="request">The <see cref="SaveSequenceQueryRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(SaveSequenceQueryRequest request, CallContext context = default);

    /// <summary>
    /// Delete a saved query.
    /// </summary>
    /// <param name="request">The <see cref="DeleteSequenceQueryRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(DeleteSequenceQueryRequest request, CallContext context = default);

    /// <summary>
    /// Get the folders an owner can see.
    /// </summary>
    /// <param name="request">The <see cref="GetSequenceQueriesRequest"/>.</param>
    /// <returns>A collection of <see cref="SequenceQueryFolderDefinition"/>.</returns>
    Task<IEnumerable<SequenceQueryFolderDefinition>> GetSequenceQueryFolders(GetSequenceQueriesRequest request);

    /// <summary>
    /// Save a folder, replacing any existing folder with the same identifier.
    /// </summary>
    /// <param name="request">The <see cref="SaveSequenceQueryFolderRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task SaveFolder(SaveSequenceQueryFolderRequest request, CallContext context = default);

    /// <summary>
    /// Delete a folder.
    /// </summary>
    /// <param name="request">The <see cref="DeleteSequenceQueryFolderRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task DeleteFolder(DeleteSequenceQueryFolderRequest request, CallContext context = default);
}
