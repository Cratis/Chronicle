// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_renewing : given.a_peer_enrollment
{
    bool _beforeHalfOfTheLifetime;
    bool _afterHalfOfTheLifetime;
    FederationEnrolledIdentity _original;
    FederationEnrolledIdentity _renewed;
    byte[] _originalKey;

    async Task Because()
    {
        await _peer.Redeem((await _authority.CreateEnrollment("acme", "prod")).Code);
        _original = await _identities.Get();
        _originalKey = _original.Certificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo();

        _time.Advance(TimeSpan.FromDays(6));
        _beforeHalfOfTheLifetime = await _peer.RenewIfDue();

        _time.Advance(TimeSpan.FromDays(2));
        _afterHalfOfTheLifetime = await _peer.RenewIfDue();
        _renewed = await _identities.Get();
    }

    [Fact] void should_not_renew_before_half_of_the_lifetime_has_passed() => _beforeHalfOfTheLifetime.ShouldBeFalse();
    [Fact] void should_renew_after_half_of_the_lifetime_has_passed() => _afterHalfOfTheLifetime.ShouldBeTrue();
    [Fact] void should_renew_once() => _transport.Renewals.ShouldEqual(1);
    [Fact] void should_keep_the_identity() => _renewed.Identity.ShouldEqual(_original.Identity);
    [Fact] void should_get_a_new_certificate() => _renewed.Serial.ShouldNotEqual(_original.Serial);
    [Fact] void should_use_a_new_key() => _renewed.Certificate.GetECDsaPublicKey().ExportSubjectPublicKeyInfo().ShouldNotEqual(_originalKey);
    [Fact] void should_hold_the_key_of_the_new_certificate() => _renewed.Certificate.HasPrivateKey.ShouldBeTrue();
    [Fact] void should_extend_the_expiry() => _renewed.NotAfter.ShouldBeGreaterThan(_original.NotAfter);
    [Fact] void should_have_installed_it_as_a_renewal() => string.Join(',', _identities.Reasons).ShouldEqual($"{FederationIssuanceReasons.Enrollment},{FederationIssuanceReasons.Renewal}");
}
