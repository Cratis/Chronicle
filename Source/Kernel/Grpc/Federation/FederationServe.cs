// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Federation;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Federation;

/// <summary>
/// Represents the implementation of <see cref="IFederationServe"/>.
/// </summary>
/// <remarks>
/// Anonymous with respect to OAuth, like the pull: a peer presents a client certificate, never a token, and that is decided by the serve
/// service. This is safe only because <see cref="FederationPortPolicy"/> serves this service on the federation port alone.
/// </remarks>
/// <param name="service">The <see cref="IFederationServeService"/>.</param>
internal sealed class FederationServe(IFederationServeService service) : IFederationServe
{
    /// <inheritdoc/>
    [AllowAnonymous]
    public IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, CallContext callContext = default) =>
        service.Serve(batches, callContext.CancellationToken);
}
