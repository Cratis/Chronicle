// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Resolves hosts using the operating system resolver.
/// </summary>
public class WebhookHostResolver : IWebhookHostResolver
{
    /// <inheritdoc/>
    public Task<IPAddress[]> Resolve(string host, CancellationToken cancellationToken) => Dns.GetHostAddressesAsync(host, cancellationToken);
}
