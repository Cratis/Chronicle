// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;
using DnsClient.Protocol;

namespace Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver;

public class when_resolving_srv_connection_string : Specification
{
    ILookupClient _lookupClient;
    ChronicleServerAddressResolver _resolver;
    ChronicleConnectionString _connectionString;
    IReadOnlyList<ChronicleServerAddress> _result;

    void Establish()
    {
        _lookupClient = Substitute.For<ILookupClient>();
        var response = Substitute.For<IDnsQueryResponse>();
        response.Answers.Returns(
        [
            CreateSrvRecord(priority: 10, weight: 5, port: 35001, target: "node2.example.com."),
            CreateSrvRecord(priority: 0, weight: 1, port: 35000, target: "node1.example.com."),
            CreateSrvRecord(priority: 0, weight: 10, port: 35002, target: "node3.example.com.")
        ]);
        _lookupClient.QueryAsync("_chronicle._tcp.cluster.example.com", QueryType.SRV, Arg.Any<QueryClass>(), Arg.Any<CancellationToken>()).Returns(response);
        _resolver = new ChronicleServerAddressResolver(_lookupClient);
        _connectionString = new ChronicleConnectionString("chronicle+srv://cluster.example.com");
    }

    async Task Because() => _result = await _resolver.Resolve(_connectionString);

    [Fact] void should_return_addresses_ordered_by_priority_then_weight() => _result.ShouldEqual(
        new ChronicleServerAddress("node3.example.com", 35002),
        new ChronicleServerAddress("node1.example.com", 35000),
        new ChronicleServerAddress("node2.example.com", 35001));

    static SrvRecord CreateSrvRecord(int priority, int weight, int port, string target) =>
        new(
            new ResourceRecordInfo("_chronicle._tcp.cluster.example.com.", ResourceRecordType.SRV, QueryClass.IN, 300, 0),
            (ushort)priority,
            (ushort)weight,
            (ushort)port,
            DnsString.Parse(target));
}
