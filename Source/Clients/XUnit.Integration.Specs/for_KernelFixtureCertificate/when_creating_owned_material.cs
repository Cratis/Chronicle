// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

[UnsupportedOSPlatform("windows")]
public class when_creating_owned_material : Specification
{
    KernelFixtureCertificate _material;
    X509Certificate2 _certificate;

    void Because()
    {
        _material = KernelFixtureCertificate.Create();
        _certificate = X509CertificateLoader.LoadCertificate(_material.PublicCertificate.AsSpan());
    }

    async Task Destroy()
    {
        _certificate?.Dispose();
        if (_material is not null) await _material.DisposeAfter(() => ValueTask.CompletedTask);
    }

    [Fact] void should_observe_only_public_certificate_material() => _certificate.HasPrivateKey.ShouldBeFalse();
    [Fact] void should_have_exported_material() => new FileInfo(_material.Path).Length.ShouldBeGreaterThan(0L);
    [Fact] void should_match_localhost() => _certificate.MatchesHostname("localhost").ShouldBeTrue();
    [Fact] void should_not_match_another_host() => _certificate.MatchesHostname("another-host").ShouldBeFalse();
    [Fact] void should_be_self_signed() => _certificate.Issuer.ShouldEqual(_certificate.Subject);
    [Fact] void should_use_the_existing_callbacks_exact_digest() => _material.Hash.ShouldEqual(_certificate.GetCertHashString());
    [Fact] void should_be_short_lived() => (_certificate.NotAfter.ToUniversalTime() - _certificate.NotBefore.ToUniversalTime()).ShouldBeLessThan(TimeSpan.FromDays(2));
    [Fact] void should_allow_server_authentication() => _certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single().EnhancedKeyUsages.Cast<System.Security.Cryptography.Oid>().Select(_ => _.Value).ShouldContainOnly("1.3.6.1.5.5.7.3.1");
    [Fact] void should_not_be_a_certificate_authority() => _certificate.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority.ShouldBeFalse();
    [Fact] void should_have_a_private_directory() => File.GetUnixFileMode(Path.GetDirectoryName(_material.Path)!).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    [Fact] void should_have_a_private_file() => File.GetUnixFileMode(_material.Path).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
}
