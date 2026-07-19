// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

/// <summary>
/// A test double for the connection-count probe's <see cref="HttpClient"/> - answers connection
/// count probes with a canned count per host plus any reservations made against that host, tracks
/// reservation POSTs, and can simulate an unreachable host.
/// </summary>
/// <param name="connectionCountsByHost">The base connection count to answer with, keyed by host.</param>
/// <param name="unreachableHosts">Hosts that should simulate being unreachable.</param>
public class FakeConnectionCountHttpMessageHandler(
    IReadOnlyDictionary<string, int> connectionCountsByHost,
    IReadOnlySet<string>? unreachableHosts = null) : HttpMessageHandler
{
    readonly Dictionary<string, int> _reservationsByHost = [];

    /// <summary>
    /// Gets the number of reservations made against a host.
    /// </summary>
    /// <param name="host">The host to get the reservation count for.</param>
    /// <returns>The number of reservations made.</returns>
    public int ReservationsFor(string host) => _reservationsByHost.GetValueOrDefault(host);

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri!.Host;
        if (unreachableHosts?.Contains(host) == true)
        {
            throw new HttpRequestException($"Simulated unreachable host '{host}'");
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        if (request.Method == HttpMethod.Post)
        {
            _reservationsByHost[host] = _reservationsByHost.GetValueOrDefault(host) + 1;
            return Task.FromResult(response);
        }

        var count = connectionCountsByHost[host] + _reservationsByHost.GetValueOrDefault(host);
        response.Content = new StringContent(count.ToString());
        return Task.FromResult(response);
    }
}
