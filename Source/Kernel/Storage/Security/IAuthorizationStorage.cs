// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines a storage interface for OAuth authorizations.
/// </summary>
public interface IAuthorizationStorage
{
    /// <summary>
    /// Gets an authorization by its unique identifier.
    /// </summary>
    /// <param name="id">The authorization's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization if found, null otherwise.</returns>
    Task<Authorization?> GetById(AuthorizationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new authorization.
    /// </summary>
    /// <param name="authorization">The authorization to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Create(Authorization authorization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing authorization.
    /// </summary>
    /// <param name="authorization">The authorization to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(Authorization authorization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an authorization.
    /// </summary>
    /// <param name="id">The authorization's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(AuthorizationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of authorizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count.</returns>
    Task<long> Count(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists authorizations with pagination.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorizations.</returns>
    Task<IEnumerable<Authorization>> List(int? count, int? offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds authorizations by application ID.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching authorizations.</returns>
    Task<IEnumerable<Authorization>> FindByApplicationId(ApplicationId applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds authorizations by subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching authorizations.</returns>
    Task<IEnumerable<Authorization>> FindBySubject(Subject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds authorizations by application ID and subject.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching authorizations.</returns>
    Task<IEnumerable<Authorization>> FindByApplicationIdAndSubject(ApplicationId applicationId, Subject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds authorizations by application ID, subject, and status.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="status">The status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching authorizations.</returns>
    Task<IEnumerable<Authorization>> FindByApplicationIdSubjectAndStatus(ApplicationId applicationId, Subject subject, AuthorizationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes authorizations.
    /// </summary>
    /// <param name="threshold">The threshold date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of pruned authorizations.</returns>
    Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default);
}
