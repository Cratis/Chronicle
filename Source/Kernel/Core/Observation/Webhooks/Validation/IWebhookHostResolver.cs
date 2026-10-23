// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Resolves webhook hosts at registration and immediately before connecting.
/// </summary>
public interface IWebhookHostResolver
{
    /// <summary>
    /// Resolves every address for a host.
    /// </summary>
    /// <param name="host">The host to resolve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved addresses.</returns>
    Task<IPAddress[]> Resolve(string host, CancellationToken cancellationToken);
}
