// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_a_ca_certificate_path : Specification
{
    ChronicleConnectionString _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://host:35000/?skipTlsValidation=false&caCertificatePath=/certs/ca.pem");

    [Fact] void should_have_the_ca_certificate_path() => _result.CaCertificatePath.ShouldEqual("/certs/ca.pem");
    [Fact] void should_not_skip_tls_validation() => _result.SkipTlsValidation.ShouldBeFalse();
}
