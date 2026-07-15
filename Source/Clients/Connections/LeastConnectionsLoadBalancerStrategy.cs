// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancerStrategy"/> that asks each candidate
/// server how many clients it currently has connected and selects the one with the fewest.
/// </summary>
/// <remarks>
/// Independent, randomly-seeded strategies like round-robin can't guarantee an even spread for a
/// small fleet - two clients picking independently among two servers collide about half the time.
/// This strategy asks each candidate directly instead of guessing, and reserves a slot on the
/// server it picks before returning - closing the race where a second client probes while the
/// first is still mid-handshake (TLS plus a compatibility check) and hasn't registered as connected
/// yet. A small random delay before every probe further protects against a fleet that starts every
/// instance at once (a rollout, this composition), or reconnects at once (every instance was
/// waiting for the same server to come back and now retries in lockstep): without it, sibling
/// instances can still probe within microseconds of each other, before either has reserved
/// anything, and tie. The jitter is not limited to the first attempt for exactly this reason - a
/// synchronized retry after a shared failure needs the same protection a cold start does.
/// </remarks>
public class LeastConnectionsLoadBalancerStrategy : ILoadBalancerStrategy, IDisposable
{
    /// <summary>
    /// The well-known name of the strategy, usable as the loadBalancer option in a connection string.
    /// </summary>
    public const string StrategyName = "least-connections";

    const string ConnectionCountRoute = "connections/count";
    const string ReserveConnectionRoute = "connections/reserve";
    static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(2);
    static readonly TimeSpan _defaultMaxSelectionJitter = TimeSpan.FromMilliseconds(250);

    readonly bool _ownsHttpClient;
    readonly HttpMessageHandler? _httpMessageHandler;
    readonly HttpClient _httpClient;
    readonly string _scheme;
    readonly int _maxSelectionJitterMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeastConnectionsLoadBalancerStrategy"/> class.
    /// </summary>
    /// <param name="disableTls">Whether TLS is disabled for the connection.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> to probe candidate servers with. Defaults to one that accepts development self-signed certificates.</param>
    /// <param name="maxSelectionJitter">Optional upper bound for the random delay before every probe. Defaults to 250ms; pass <see cref="TimeSpan.Zero"/> to disable it.</param>
    public LeastConnectionsLoadBalancerStrategy(bool disableTls, HttpClient? httpClient = null, TimeSpan? maxSelectionJitter = null)
    {
        _scheme = disableTls ? "http" : "https";
        _maxSelectionJitterMilliseconds = (int)(maxSelectionJitter ?? _defaultMaxSelectionJitter).TotalMilliseconds;
        _ownsHttpClient = httpClient is null;

        if (httpClient is not null)
        {
            _httpClient = httpClient;
        }
        else
        {
            _httpMessageHandler = new SocketsHttpHandler
            {
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, chain, sslPolicyErrors) =>
                        DevelopmentCertificateValidation.AcceptSelfSigned(chain, sslPolicyErrors)
                }
            };
            _httpClient = new HttpClient(_httpMessageHandler);
        }
    }

    /// <inheritdoc/>
    public async Task<ChronicleServerAddress> Next(IReadOnlyList<ChronicleServerAddress> serverAddresses)
    {
        if (serverAddresses.Count == 0)
        {
            throw new MissingServerAddress();
        }

        if (serverAddresses.Count == 1)
        {
            return serverAddresses[0];
        }

        if (_maxSelectionJitterMilliseconds > 0)
        {
            await Task.Delay(Random.Shared.Next(_maxSelectionJitterMilliseconds));
        }

        var connectionCounts = await Task.WhenAll(serverAddresses.Select(GetConnectionCount));

        // Break ties randomly rather than always preferring the first candidate - a fleet that
        // starts all its instances at once (a rollout, this composition) has every instance probe
        // before any of them have registered as connected, so they'd all see the same counts and,
        // with a deterministic tie-break, all pick the exact same server.
        var minimumCount = connectionCounts.Min();
        var leastLoadedIndices = Enumerable.Range(0, connectionCounts.Length)
            .Where(index => connectionCounts[index] == minimumCount)
            .ToArray();

        var selected = serverAddresses[leastLoadedIndices[Random.Shared.Next(leastLoadedIndices.Length)]];
        await ReserveConnection(selected);
        return selected;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
            _httpMessageHandler?.Dispose();
        }
    }

    async Task<int> GetConnectionCount(ChronicleServerAddress serverAddress)
    {
        try
        {
            using var timeout = new CancellationTokenSource(_probeTimeout);
            var address = new Uri($"{_scheme}://{serverAddress.Host}:{serverAddress.Port}/{ConnectionCountRoute}");
            var response = await _httpClient.GetAsync(address, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return int.MaxValue;
            }

            var content = await response.Content.ReadAsStringAsync(timeout.Token);
            return int.TryParse(content, out var count) ? count : int.MaxValue;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Unreachable or too slow to answer - treat as maximally loaded so it is never picked
            // over a server that actually responded, rather than failing the whole selection.
            return int.MaxValue;
        }
    }

    async Task ReserveConnection(ChronicleServerAddress serverAddress)
    {
        try
        {
            using var timeout = new CancellationTokenSource(_probeTimeout);
            var address = new Uri($"{_scheme}://{serverAddress.Host}:{serverAddress.Port}/{ReserveConnectionRoute}");
            await _httpClient.PostAsync(address, content: null, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Best-effort - the selection still proceeds against the server that was picked. The
            // worst case is a concurrent probe not seeing this pick reflected yet, same as before
            // reservation existed.
        }
    }
}
