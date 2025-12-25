// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines a storage interface for OAuth applications.
/// </summary>
public interface IApplicationStorage
{
    /// <summary>
    /// Gets an application by its unique identifier.
    /// </summary>
    /// <param name="id">The application's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The application if found, null otherwise.</returns>
    Task<Application?> GetById(ApplicationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an application by its client ID.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The application if found, null otherwise.</returns>
    Task<Application?> GetByClientId(ClientId clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new application.
    /// </summary>
    /// <param name="application">The application to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Create(Application application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing application.
    /// </summary>
    /// <param name="application">The application to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(Application application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an application.
    /// </summary>
    /// <param name="id">The application's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ApplicationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all applications.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All applications.</returns>
    Task<IEnumerable<Application>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of applications.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count.</returns>
    Task<long> Count(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists applications with pagination.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Applications.</returns>
    Task<IEnumerable<Application>> List(int? count, int? offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds applications by redirect URI.
    /// </summary>
    /// <param name="redirectUri">The redirect URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching applications.</returns>
    Task<IEnumerable<Application>> FindByRedirectUri(string redirectUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds applications by post-logout redirect URI.
    /// </summary>
    /// <param name="postLogoutRedirectUri">The post-logout redirect URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching applications.</returns>
    Task<IEnumerable<Application>> FindByPostLogoutRedirectUri(string postLogoutRedirectUri, CancellationToken cancellationToken = default);
}
