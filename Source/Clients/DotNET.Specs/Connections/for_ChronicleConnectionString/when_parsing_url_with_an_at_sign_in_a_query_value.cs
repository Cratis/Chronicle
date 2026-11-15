// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_an_at_sign_in_a_query_value : Specification
{
    ChronicleConnectionString _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://host:35000/?certificatePath=/certs/a@b.pfx");

    [Fact] void should_parse_the_host() => _result.ServerAddress.Host.ShouldEqual("host");
    [Fact] void should_keep_the_query_value() => _result.CertificatePath.ShouldEqual("/certs/a@b.pfx");
}
