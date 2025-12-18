// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines a storage interface for OAuth scopes.
/// </summary>
public interface IScopeStorage
{
    /// <summary>
    /// Gets a scope by its unique identifier.
    /// </summary>
    /// <param name="id">The scope's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scope if found, null otherwise.</returns>
    Task<Scope?> GetById(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scope by its name.
    /// </summary>
    /// <param name="name">The scope name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scope if found, null otherwise.</returns>
    Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new scope.
    /// </summary>
    /// <param name="scope">The scope to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Create(Scope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scope.
    /// </summary>
    /// <param name="scope">The scope to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(Scope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scope.
    /// </summary>
    /// <param name="id">The scope's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of scopes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count.</returns>
    Task<long> Count(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists scopes with pagination.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scopes.</returns>
    Task<IEnumerable<Scope>> List(int? count, int? offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds scopes by resource.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching scopes.</returns>
    Task<IEnumerable<Scope>> FindByResource(string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds scopes by names.
    /// </summary>
    /// <param name="names">The scope names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching scopes.</returns>
    Task<IEnumerable<Scope>> FindByNames(IEnumerable<string> names, CancellationToken cancellationToken = default);
}
