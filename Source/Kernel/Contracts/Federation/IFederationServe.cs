// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Federation;

/// <summary>
/// Defines the gRPC service contract a dialing kernel uses to produce events to the kernel it dialed, over the same connection it pulls on.
/// </summary>
/// <remarks>
/// The call is hand-written because a bidirectional stream has no Arc artifact to derive it from. The acceptor decides the target from the
/// policy of the certificate's subject, so nothing in the frames names one. The existing <see cref="IFederationPull"/> is unchanged.
/// </remarks>
[Service]
public interface IFederationServe
{
    /// <summary>
    /// Serves the dialer's own outbox. The acceptor sends pull requests as frames, and the dialer answers each with a batch.
    /// </summary>
    /// <param name="batches">The batches the dialer answers with, in the order the requests were sent.</param>
    /// <param name="callContext">The gRPC call context.</param>
    /// <returns>The pull requests of the acceptor.</returns>
    [Operation]
    IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, CallContext callContext = default);
}
