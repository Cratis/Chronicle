// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_tls_is_enabled_with_a_configured_certificate : Cratis.Chronicle.Server.for_CertificateLoader.given.a_certificate_file
{
    X509Certificate2? _selected;

    void Establish() => WritePkcs12(password: null);

    void Because() => _selected = KernelCertificateSelection.Select(
        OptionsWithTls(password: null),
        Substitute.For<ILogger<Kernel>>(),
        developmentBuild: false);

    [Fact] void should_select_the_tls_certificate() => _selected!.Thumbprint.ShouldEqual(_sourceCertificate.Thumbprint);

    void Destroy() => _selected?.Dispose();
}
