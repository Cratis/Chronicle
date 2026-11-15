// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_another_instance_of_the_subject_is_enrolled : given.a_peer_admission
{
    FederationAdmission _older;
    FederationAdmission _newer;

    async Task Because()
    {
        var first = await Issued("acme", "one");
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await Issued("acme", "two");
        await SetPolicy("acme");

        _older = await Admit(first);
        _newer = await Admit(second);
    }

    [Fact] void should_admit_the_newest_instance() => _newer.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_admit_the_older_instance_so_the_two_cannot_evict_each_other() => _older.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_the_older_instance_was_superseded() => _older.Refusal.ShouldEqual(FederationAdmissionRefusal.Superseded);
}
