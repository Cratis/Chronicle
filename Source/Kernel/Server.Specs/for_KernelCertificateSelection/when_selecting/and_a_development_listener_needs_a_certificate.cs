// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_a_development_listener_needs_a_certificate : Specification
{
    X509Certificate2? _certificate;

    void Because() => _certificate = KernelCertificateSelection.Select(
        new(),
        Substitute.For<ILogger<Kernel>>(),
        developmentBuild: true);

    [Fact] void should_generate_a_certificate_with_a_private_key() => _certificate!.HasPrivateKey.ShouldBeTrue();

    void Destroy() => _certificate?.Dispose();
}
