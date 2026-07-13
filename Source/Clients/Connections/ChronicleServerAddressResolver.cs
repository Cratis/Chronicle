// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;
using DnsClient.Protocol;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="IChronicleServerAddressResolver"/> that resolves
/// addresses directly from the connection string, or through DNS SRV records for chronicle+srv
/// connection strings.
/// </summary>
/// <param name="lookupClient">Optional <see cref="ILookupClient"/> to use for DNS lookups. Defaults to a system-configured <see cref="LookupClient"/>.</param>
public class ChronicleServerAddressResolver(ILookupClient? lookupClient = null) : IChronicleServerAddressResolver
{
    /// <summary>
    /// The DNS SRV service and protocol prefix used when looking up Chronicle servers.
    /// </summary>
    public const string SrvServicePrefix = "_chronicle._tcp";

    readonly Lazy<ILookupClient> _lookupClient = new(() => lookupClient ?? new LookupClient());

    /// <inheritdoc/>
    /// <exception cref="NoSrvRecordsFound">Thrown when a chronicle+srv connection string yields no SRV records.</exception>
    public async Task<IReadOnlyList<ChronicleServerAddress>> Resolve(ChronicleConnectionString connectionString)
    {
        if (!connectionString.IsSrv)
        {
            return connectionString.ServerAddresses;
        }

        var serviceName = $"{SrvServicePrefix}.{connectionString.ServerAddress.Host}";
        var response = await _lookupClient.Value.QueryAsync(serviceName, QueryType.SRV);
        var addresses = response.Answers
            .OfType<SrvRecord>()
            .OrderBy(record => record.Priority)
            .ThenByDescending(record => record.Weight)
            .Select(record => new ChronicleServerAddress(record.Target.Value.TrimEnd('.'), record.Port))
            .ToArray();

        return addresses.Length == 0 ? throw new NoSrvRecordsFound(serviceName) : addresses;
    }
}
