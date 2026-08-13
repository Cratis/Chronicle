// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Defines the contract for working with saved event sequence queries.
/// </summary>
[Service]
public interface ISequenceQueries
{
    /// <summary>
    /// Get the saved queries an owner can see.
    /// </summary>
    /// <param name="request">The <see cref="GetSequenceQueriesRequest"/>.</param>
    /// <returns>A collection of <see cref="SequenceQueryDefinition"/>.</returns>
    [Operation]
    Task<IEnumerable<SequenceQueryDefinition>> GetSequenceQueries(GetSequenceQueriesRequest request);

    /// <summary>
    /// Observe the saved queries an owner can see.
    /// </summary>
    /// <param name="request">The <see cref="GetSequenceQueriesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An <see cref="IObservable{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="SequenceQueryDefinition"/>.</returns>
    [Operation]
    IObservable<IEnumerable<SequenceQueryDefinition>> ObserveSequenceQueries(GetSequenceQueriesRequest request, CallContext context = default);

    /// <summary>
    /// Save a query, replacing any existing query with the same identifier.
    /// </summary>
    /// <param name="request">The <see cref="SaveSequenceQueryRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Save(SaveSequenceQueryRequest request, CallContext context = default);

    /// <summary>
    /// Delete a saved query.
    /// </summary>
    /// <param name="request">The <see cref="DeleteSequenceQueryRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Delete(DeleteSequenceQueryRequest request, CallContext context = default);
}
