// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationAuthorityKeys;

public class when_getting_the_authority : Specification
{
    IEncryptionKeyStorage _custody;
    EncryptionKey _stored;
    given.KeyEncryptionKeyFile _kekFiles;
    FederationKeyProtection _protection;
    FederationAuthorityKeys _first;
    FederationAuthorityKeys _secondSilo;
    (System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate, bool Created) _created;
    (System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate, bool Created) _again;
    (System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate, bool Created) _fromTheSecondSilo;

    void Establish()
    {
        _custody = Substitute.For<IEncryptionKeyStorage>();
        _custody.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationAuthority, Arg.Any<EncryptionKey>())
            .Returns(call => Task.FromResult(_stored ??= call.Arg<EncryptionKey>()));
        var options = Options.Create(new ChronicleOptions { Federation = new() { Hosted = new() { Enabled = true, TrustDomain = "example" } } });
        _kekFiles = new();
        _protection = given.KeyEncryptionKeyFile.Protection(_kekFiles.Create());
        _first = new(_custody, _protection, options, TimeProvider.System);
        _secondSilo = new(_custody, _protection, options, TimeProvider.System);
    }

    void Destroy() => _kekFiles.Dispose();

    async Task Because()
    {
        _created = await _first.GetOrCreate();
        _again = await _first.GetOrCreate();
        _fromTheSecondSilo = await _secondSilo.GetOrCreate();
    }

    [Fact] void should_create_the_authority_the_first_time() => _created.Created.ShouldBeTrue();
    [Fact] void should_not_create_it_again() => _again.Created.ShouldBeFalse();
    [Fact] void should_not_create_it_on_another_silo_that_finds_it_in_the_custody() => _fromTheSecondSilo.Created.ShouldBeFalse();
    [Fact] void should_converge_on_one_authority() => FederationCertificateAuthority.Fingerprint(_fromTheSecondSilo.Certificate).ShouldEqual(FederationCertificateAuthority.Fingerprint(_created.Certificate));
    [Fact] void should_not_hand_the_private_key_to_the_custody_in_the_clear() => Catch.Exception(() => ECDsa.Create().ImportPkcs8PrivateKey(_stored.Private, out _)).ShouldNotBeNull();
    [Fact] void should_hand_the_private_key_to_the_custody() => _stored.Private.ShouldNotBeEmpty();
    [Fact] void should_hand_only_the_certificate_as_the_public_part() => _stored.Public.ShouldEqual(_created.Certificate.RawData);
    [Fact] void should_be_able_to_sign_with_the_key_in_the_custody() => _created.Certificate.GetECDsaPrivateKey().ShouldNotBeNull();
    [Fact] void should_be_a_certificate_authority_that_can_only_sign() => _created.Certificate.Extensions.OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>().Single().CertificateAuthority.ShouldBeTrue();
    [Fact] void should_hold_a_key_that_matches_the_certificate() => KeyInCustody().ExportSubjectPublicKeyInfo().ShouldEqual(_created.Certificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo());

    ECDsa KeyInCustody()
    {
        var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(_protection.Unprotect(_stored.Private, "authority", "federation-authority", _created.Certificate.SerialNumber).GetAwaiter().GetResult(), out _);
        return key;
    }
}
