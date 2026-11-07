// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_loading_the_certificate;

public class and_the_native_password_is_wrong : given.a_certificate_file
{
    const string Password = "synthetic-native-wrong-password-marker";
    Exception _failure;
    X509Certificate2 _result;
    X509Certificate2 _nativeResult;
    Exception _nativeFailure;

    void Establish()
    {
        _certificatePath = Path.Combine(Path.GetTempPath(), $"synthetic-certificate-password-marker-{Guid.NewGuid():N}.pfx");
        WritePkcs12("synthetic-native-original-password-marker");
    }

    void Because()
    {
#if NET8_0
        _nativeFailure = Catch.Exception(() => _nativeResult = new X509Certificate2(_certificatePath, Password));
#else
        _nativeFailure = Catch.Exception(() => _nativeResult = X509CertificateLoader.LoadPkcs12FromFile(_certificatePath, Password));
#endif
        _failure = Catch.Exception(() => _result = CertificateLoader.LoadCertificate(_certificatePath, Password));
    }

    void Destroy()
    {
        _nativeResult?.Dispose();
        _result?.Dispose();
    }

    [Fact] void should_observe_a_native_cryptographic_failure() => (_nativeFailure is CryptographicException).ShouldBeTrue();
    [Fact] void should_preserve_the_actual_native_failure_type() => _failure.GetType().ShouldEqual(_nativeFailure.GetType());
    [Fact] void should_not_expose_certificate_path_markers() => diagnostic_surfaces.ShouldContainNoMarkers(diagnostic_surfaces.ExceptionText(_failure));
    [Fact] void should_not_expose_the_full_path() => diagnostic_surfaces.ExceptionText(_failure).Any(text => text.Contains(_certificatePath, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_expose_the_supplied_password() => diagnostic_surfaces.ExceptionText(_failure).Any(text => text.Contains(Password, StringComparison.Ordinal)).ShouldBeFalse();
}
