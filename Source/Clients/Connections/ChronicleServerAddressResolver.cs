// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DnsClient;
using DnsClient.Protocol;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="IChronicleServerAddressResolver"/> that resolves
/// addresses directly from the connection string, or through DNS SRV records for chronicle+srv
/// connection strings.
/// </summary>
/// <param name="lookupClient">Optional <see cref="ILookupClient"/> to use for DNS lookups. Defaults to a system-configured <see cref="LookupClient"/>, or one targeting the connection string's srvNameServer when specified.</param>
public class ChronicleServerAddressResolver(ILookupClient? lookupClient = null) : IChronicleServerAddressResolver
{
    /// <summary>
    /// The DNS SRV service and protocol prefix used when looking up Chronicle servers.
    /// </summary>
    public const string SrvServicePrefix = "_chronicle._tcp";

    const int DefaultDnsPort = 53;

    readonly Lazy<ILookupClient> _systemLookupClient = new(() => new LookupClient());
    readonly ConcurrentDictionary<string, ILookupClient> _lookupClientsByNameServer = new();

    /// <inheritdoc/>
    /// <exception cref="NoSrvRecordsFound">Thrown when a chronicle+srv connection string yields no SRV records.</exception>
    public async Task<IReadOnlyList<ChronicleServerAddress>> Resolve(ChronicleConnectionString connectionString)
    {
        if (!connectionString.IsSrv)
        {
            return connectionString.ServerAddresses;
        }

        var serviceName = $"{SrvServicePrefix}.{connectionString.ServerAddress.Host}";
        var response = await GetLookupClientFor(connectionString).QueryAsync(serviceName, QueryType.SRV);
        var addresses = response.Answers
            .OfType<SrvRecord>()
            .OrderBy(record => record.Priority)
            .ThenByDescending(record => record.Weight)
            .Select(record => new ChronicleServerAddress(record.Target.Value.TrimEnd('.'), record.Port))
            .ToArray();

        return addresses.Length == 0 ? throw new NoSrvRecordsFound(serviceName) : addresses;
    }

    static ILookupClient CreateLookupClientFor(string nameServer)
    {
        var colonIndex = nameServer.LastIndexOf(':');
        var host = colonIndex == -1 ? nameServer : nameServer[..colonIndex];
        var port = colonIndex == -1 ? DefaultDnsPort : int.Parse(nameServer[(colonIndex + 1)..]);
        var address = IPAddress.TryParse(host, out var parsedAddress)
            ? parsedAddress
            : Dns.GetHostAddresses(host).First(hostAddress => hostAddress.AddressFamily == AddressFamily.InterNetwork);

        return new LookupClient(new IPEndPoint(address, port));
    }

    ILookupClient GetLookupClientFor(ChronicleConnectionString connectionString)
    {
        if (lookupClient is not null)
        {
            return lookupClient;
        }

        var nameServer = connectionString.SrvNameServer;
        if (string.IsNullOrEmpty(nameServer))
        {
            return _systemLookupClient.Value;
        }

        return _lookupClientsByNameServer.GetOrAdd(nameServer, CreateLookupClientFor);
    }
}
