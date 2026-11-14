// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Federation.for_FederationEnrolledIdentities;

public class when_installing_an_identity : Specification
{
    given.FakeCertificateRegistry _registry;
    IEncryptionKeyStorage _custody;
    EncryptionKey _protectionSecret;
    FederationEnrolledIdentities _identities;
    X509Certificate2 _authority;
    X509Certificate2 _certificate;
    ECDsa _key;
    FederationEnrolledIdentity _loaded;
    FederationEnrolledIdentities _afterRestart;
    FederationEnrolledIdentity _loadedAfterRestart;
    FederationIdentityInstalled _event;
    string _eventAsJson;
    byte[] _pkcs8;

    void Establish()
    {
        _registry = new();
        _custody = Substitute.For<IEncryptionKeyStorage>();
        _custody.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationKeyProtection, Arg.Any<EncryptionKey>())
            .Returns(call => Task.FromResult(_protectionSecret ??= call.Arg<EncryptionKey>()));
        _identities = new(_registry, _custody);
        _afterRestart = new(_registry, _custody);

        _authority = FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _pkcs8 = _key.ExportPkcs8PrivateKey();
        var request = new CertificateRequest("CN=peer", _key, HashAlgorithmName.SHA256).CreateSigningRequest();
        _certificate = FederationCertificateAuthority.IssueClient(_authority, request, "spiffe://example/customer/acme/kernel/prod", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(14));
    }

    void Destroy()
    {
        _authority.Dispose();
        _certificate.Dispose();
        _key.Dispose();
    }

    async Task Because()
    {
        await _identities.Install(_key, _certificate, _authority, "https://federation.example.com:35100/", FederationIssuanceReasons.Enrollment);
        _event = _registry.Appended.Select(_ => _.Event).OfType<FederationIdentityInstalled>().Single();
        _eventAsJson = JsonSerializer.Serialize(_event);
        _loaded = await _identities.Get();
        _loadedAfterRestart = await _afterRestart.Get();
    }

    [Fact] void should_record_the_identity() => _event.Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_record_the_serial_number_and_expiry() => (_event.Serial, _event.NotAfter).ShouldEqual((_certificate.SerialNumber, new DateTimeOffset(_certificate.NotAfter)));
    [Fact] void should_keep_the_protection_secret_in_the_custody() => _protectionSecret.Private.Length.ShouldEqual(32);
    [Fact] void should_not_record_the_private_key_in_the_clear() => _eventAsJson.ShouldNotContain(Convert.ToBase64String(_pkcs8));
    [Fact] void should_not_record_the_private_key_bytes() => Convert.FromBase64String(_event.ProtectedPrivateKey).AsSpan().IndexOf(_pkcs8.AsSpan(0, 32)).ShouldEqual(-1);
    [Fact] void should_not_record_the_protection_secret() => _eventAsJson.ShouldNotContain(Convert.ToBase64String(_protectionSecret.Private));
    [Fact] void should_load_the_identity_with_a_working_key() => _loaded.Certificate.GetECDsaPrivateKey().SignData([1, 2, 3], HashAlgorithmName.SHA256).ShouldNotBeEmpty();
    [Fact] void should_load_the_same_key() => _loaded.Certificate.GetECDsaPrivateKey().ExportPkcs8PrivateKey().ShouldEqual(_pkcs8);
    [Fact] void should_load_the_identity_after_a_restart() => _loadedAfterRestart.Serial.ShouldEqual(_certificate.SerialNumber);
}
