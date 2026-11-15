// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_converting_to_string_with_a_ca_certificate_path : Specification
{
    ChronicleConnectionString _result;

    void Because() => _result = new ChronicleConnectionString(new ChronicleConnectionStringBuilder
    {
        Host = "host",
        CaCertificatePath = "/certs/ca bundle.pem"
    }.Build());

    [Fact] void should_keep_the_ca_certificate_path() => _result.CaCertificatePath.ShouldEqual("/certs/ca bundle.pem");
}
