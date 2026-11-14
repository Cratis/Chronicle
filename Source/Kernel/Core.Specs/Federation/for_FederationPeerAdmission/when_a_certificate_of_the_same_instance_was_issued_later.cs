// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_a_certificate_of_the_same_instance_was_issued_later : given.a_peer_admission
{
    FederationAdmission _older;
    FederationAdmission _newer;

    async Task Because()
    {
        // The same instance enrolled twice; the first enrollment was never revoked, so its certificate is still valid.
        var first = await Issued("acme", "prod");
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await Issued("acme", "prod");
        await SetPolicy("acme");

        _older = await Admit(first);
        _newer = await Admit(second);
    }

    [Fact] void should_admit_the_newer_certificate() => _newer.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_let_the_older_certificate_take_the_session() => _older.Refusal.ShouldEqual(FederationAdmissionRefusal.Superseded);
}
