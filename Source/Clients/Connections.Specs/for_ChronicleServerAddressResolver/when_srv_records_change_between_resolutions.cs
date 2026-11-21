// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;
using DnsClient.Protocol;

namespace Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver;

public class when_srv_records_change_between_resolutions : Specification
{
    ChronicleServerAddressResolver _resolver;
    IReadOnlyList<ChronicleServerAddress> _first;
    IReadOnlyList<ChronicleServerAddress> _second;

    void Establish()
    {
        var lookupClient = Substitute.For<ILookupClient>();
        var original = ResponseFor("node-a.example.invalid.");
        var replacement = ResponseFor("node-b.example.invalid.");
        lookupClient.QueryAsync(
            Arg.Any<DnsQuestion>(),
            Arg.Is<DnsQueryAndServerOptions>(options => !options.UseCache),
            Arg.Any<CancellationToken>()).Returns(original, replacement);
        _resolver = new ChronicleServerAddressResolver(lookupClient);
    }

    async Task Because()
    {
        var connectionString = new ChronicleConnectionString("chronicle+srv://cluster.example.invalid");
        _first = await _resolver.Resolve(connectionString);
        _second = await _resolver.Resolve(connectionString);
    }

    [Fact] void should_use_the_original_record_on_the_first_resolution() => _first.ShouldEqual(new ChronicleServerAddress("node-a.example.invalid", 35000));
    [Fact] void should_use_the_replacement_record_on_the_next_resolution() => _second.ShouldEqual(new ChronicleServerAddress("node-b.example.invalid", 35000));

    static IDnsQueryResponse ResponseFor(string target)
    {
        var response = Substitute.For<IDnsQueryResponse>();
        response.Answers.Returns(
        [
            new SrvRecord(
                new ResourceRecordInfo("_chronicle._tcp.cluster.example.invalid.", ResourceRecordType.SRV, QueryClass.IN, 300, 0),
                0,
                0,
                35000,
                DnsString.Parse(target))
        ]);
        return response;
    }
}
