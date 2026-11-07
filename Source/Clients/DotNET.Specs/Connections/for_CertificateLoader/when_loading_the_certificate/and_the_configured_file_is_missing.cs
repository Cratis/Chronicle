// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_the_certificate;

public class and_the_configured_file_is_missing : Specification
{
    string _path;
    Exception _failure;
    X509Certificate2 _result;

    void Establish() => _path = Path.Combine(Path.GetTempPath(), $"synthetic-certificate-missing-marker-{Guid.NewGuid():N}.pfx");

    void Because() => _failure = Catch.Exception(() => _result = CertificateLoader.LoadCertificate(_path, "synthetic-probe-password-marker"));

    void Destroy() => _result?.Dispose();

    [Fact] void should_preserve_the_missing_certificate_failure_type() => _failure.ShouldBeOfExactType<CertificateDoesNotExist>();
    [Fact] void should_not_expose_the_path_in_exception_surfaces() => diagnostic_surfaces.ShouldContainNoMarkers(diagnostic_surfaces.ExceptionText(_failure));
    [Fact] void should_not_expose_the_full_path() => diagnostic_surfaces.ExceptionText(_failure).Any(text => text.Contains(_path, StringComparison.Ordinal)).ShouldBeFalse();
}
