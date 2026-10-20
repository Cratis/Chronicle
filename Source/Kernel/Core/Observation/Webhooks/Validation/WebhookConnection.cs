// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Resolves, validates, then connects to the exact validated address, preventing DNS rebinding.
/// </summary>
/// <param name="resolver">The host resolver.</param>
/// <param name="policy">The target policy.</param>
public class WebhookConnection(IWebhookHostResolver resolver, IWebhookTargetPolicy policy)
{
    /// <summary>
    /// Opens a validated connection for a webhook or OAuth request.
    /// </summary>
    /// <param name="context">The HTTP connection context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connected stream.</returns>
    /// <exception cref="WebhookTargetRejected">The resolved addresses violate policy.</exception>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The returned NetworkStream owns the socket; every failed connection disposes it.")]
    public async ValueTask<Stream> Connect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var request = context.InitialRequestMessage;
        var addresses = await resolver.Resolve(context.DnsEndPoint.Host, cancellationToken);
        request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var eventStore);
        var result = policy.CheckAddresses(request.RequestUri!, addresses, eventStore);
        if (!result.IsAllowed) throw new WebhookTargetRejected(result.Reason);

        // A metadata exception belongs to this store only, including in Warn mode. Do not lend its
        // connection to a later request from another store. These clients use HTTP/1.1.
        if (addresses.Any(WebhookAddressRules.IsMetadata)) request.Headers.ConnectionClose = true;

        // Connections are pooled per authority. The delegating handler checks the per-store authority on
        // every request; this callback validates the actual DNS answers whenever a new connection is made.
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException) when (!address.Equals(addresses[^1]))
            {
                socket.Dispose();
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw new WebhookTargetRejected("Webhook target host resolved to no usable addresses.");
    }
}
