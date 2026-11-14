// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_policy_is_suspended_and_resumed : given.a_peer_admission
{
    FederationAdmission _beforeSuspension;
    FederationAdmission _whileSuspended;
    FederationAdmission _otherSubjectWhileSuspended;
    FederationAdmission _afterResume;

    async Task Because()
    {
        var acme = await Issued("acme", "prod");
        var globex = await Issued("globex", "prod");
        await SetPolicy("acme");
        await SetPolicy("globex");

        _beforeSuspension = await Admit(acme);
        await _policies.Suspend("acme", "operator");
        _whileSuspended = await Admit(acme);
        _otherSubjectWhileSuspended = await Admit(globex);
        await _policies.Resume("acme", "operator");
        _afterResume = await Admit(acme);
    }

    [Fact] void should_admit_before_the_suspension() => _beforeSuspension.IsAdmitted.ShouldBeTrue();
    [Fact] void should_refuse_at_once_while_suspended() => _whileSuspended.Refusal.ShouldEqual(FederationAdmissionRefusal.Suspended);
    [Fact] void should_not_affect_another_subject() => _otherSubjectWhileSuspended.IsAdmitted.ShouldBeTrue();
    [Fact] void should_admit_again_after_resuming() => _afterResume.IsAdmitted.ShouldBeTrue();
}
