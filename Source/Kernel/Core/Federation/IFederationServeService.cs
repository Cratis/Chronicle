// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the acceptor side of a dialer-initiated session in which the dialer produces: the acceptor sends pull requests as frames,
/// the dialer answers with batches from its own outbox, and the acceptor appends them to the inbox its policy names.
/// </summary>
/// <remarks>
/// The caller is identified solely by the client certificate of the connection, and the direction is decided by the policy entry of its
/// subject for <see cref="FederationDirection.AcceptorInbound"/>, read again before every request, so a revoked or suspended direction ends
/// the session at once without touching the other direction.
/// </remarks>
public interface IFederationServeService
{
    /// <summary>
    /// Serve one dialer.
    /// </summary>
    /// <param name="batches">The answers of the dialer.</param>
    /// <param name="cancellationToken">Cancelled when the connection ends.</param>
    /// <returns>The pull requests for the dialer, ending with a denied frame when the direction is refused.</returns>
    IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, CancellationToken cancellationToken);
}
