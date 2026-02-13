// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines a storage interface for OAuth tokens.
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Gets a token by its unique identifier.
    /// </summary>
    /// <param name="id">The token's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token if found, null otherwise.</returns>
    Task<Token?> GetById(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a token by its reference identifier.
    /// </summary>
    /// <param name="referenceId">The reference ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token if found, null otherwise.</returns>
    Task<Token?> GetByReferenceId(string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new token.
    /// </summary>
    /// <param name="token">The token to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Create(Token token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing token.
    /// </summary>
    /// <param name="token">The token to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(Token token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a token.
    /// </summary>
    /// <param name="id">The token's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count.</returns>
    Task<long> Count(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists tokens with pagination.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tokens.</returns>
    Task<IEnumerable<Token>> List(int? count, int? offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tokens by application ID.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching tokens.</returns>
    Task<IEnumerable<Token>> FindByApplicationId(string applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tokens by authorization ID.
    /// </summary>
    /// <param name="authorizationId">The authorization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching tokens.</returns>
    Task<IEnumerable<Token>> FindByAuthorizationId(string authorizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tokens by subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching tokens.</returns>
    Task<IEnumerable<Token>> FindBySubject(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tokens by application ID and subject.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching tokens.</returns>
    Task<IEnumerable<Token>> FindByApplicationIdAndSubject(string applicationId, string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds tokens by application ID, subject, and status.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="status">The status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching tokens.</returns>
    Task<IEnumerable<Token>> FindByApplicationIdSubjectAndStatus(string applicationId, string subject, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes tokens.
    /// </summary>
    /// <param name="threshold">The threshold date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of pruned tokens.</returns>
    Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default);
}
