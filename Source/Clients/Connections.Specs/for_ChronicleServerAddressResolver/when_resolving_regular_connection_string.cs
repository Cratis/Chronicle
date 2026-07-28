// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;

namespace Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver;

public class when_resolving_regular_connection_string : Specification
{
    ILookupClient _lookupClient;
    ChronicleServerAddressResolver _resolver;
    ChronicleConnectionString _connectionString;
    IReadOnlyList<ChronicleServerAddress> _result;

    void Establish()
    {
        _lookupClient = Substitute.For<ILookupClient>();
        _resolver = new ChronicleServerAddressResolver(_lookupClient);
        _connectionString = new ChronicleConnectionString("chronicle://host1:35001,host2:35002");
    }

    async Task Because() => _result = await _resolver.Resolve(_connectionString);

    [Fact] void should_return_addresses_from_connection_string() => _result.ShouldEqual(_connectionString.ServerAddresses);
    [Fact] void should_not_perform_dns_lookup() => _lookupClient.ReceivedCalls().ShouldBeEmpty();
}
