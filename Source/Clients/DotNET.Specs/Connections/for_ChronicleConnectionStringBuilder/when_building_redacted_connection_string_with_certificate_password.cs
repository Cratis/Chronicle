// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_redacted_connection_string_with_certificate_password : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            CertificatePath = "/certs/client.pfx",
            CertificatePassword = "certificate-secret"
        };
    }

    void Because() => _url = _builder.BuildRedacted();

    [Fact] void should_hide_the_certificate_path() => _url.Contains("client.pfx", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_use_opaque_diagnostics() => _url.ShouldEqual("REDACTED");
    [Fact] void should_not_expose_the_certificate_password() => _url.Contains("certificate-secret", StringComparison.Ordinal).ShouldBeFalse();
}
