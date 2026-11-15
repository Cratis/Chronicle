// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_rejecting_untrusted_connection_settings : Specification
{
    Exception[] _exceptions;

    void Because() => _exceptions =
    [
        new CertificateDoesNotExist(a_connection_string_corpus.CertificatePath),
        new InvalidCertificateOrPassword(a_connection_string_corpus.CertificatePath),
        new UnknownLoadBalancerStrategy(a_connection_string_corpus.Password),
        new NoSrvRecordsFound(a_connection_string_corpus.Username)
    ];

    [Fact] void should_not_expose_settings_in_messages_or_other_exception_surfaces() => diagnostic_surfaces.ShouldContainNoMarkers(_exceptions.SelectMany(diagnostic_surfaces.ExceptionText));
}
