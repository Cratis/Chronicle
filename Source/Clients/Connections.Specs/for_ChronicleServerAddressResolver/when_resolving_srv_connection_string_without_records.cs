// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;

namespace Cratis.Chronicle.Connections.for_ChronicleServerAddressResolver;

public class when_resolving_srv_connection_string_without_records : Specification
{
    ChronicleServerAddressResolver _resolver;
    ChronicleConnectionString _connectionString;
    Exception _exception;

    void Establish()
    {
        var lookupClient = Substitute.For<ILookupClient>();
        var response = Substitute.For<IDnsQueryResponse>();
        response.Answers.Returns([]);
        lookupClient.QueryAsync(Arg.Any<string>(), QueryType.SRV, Arg.Any<QueryClass>(), Arg.Any<CancellationToken>()).Returns(response);
        _resolver = new ChronicleServerAddressResolver(lookupClient);
        _connectionString = new ChronicleConnectionString("chronicle+srv://cluster.example.com");
    }

    async Task Because() => _exception = await Catch.Exception(() => _resolver.Resolve(_connectionString));

    [Fact] void should_throw_no_srv_records_found() => _exception.ShouldBeOfExactType<NoSrvRecordsFound>();
}
