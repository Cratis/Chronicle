// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_revocation_is_revised_between_reads : given.an_administrator_roster
{
    bool _before;
    bool _after;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        var slot = Seed(Roster.EventSourceId, new UserRevokedAdministrator(_first, "spec"));
        _before = await _roster.IsActiveAdministrator(_first);
        var revised = slot with { RevisedGeneration = EventTypeGeneration.First };
        _appended[_appended.IndexOf(slot)] = revised;
        _contents[revised] = _contents[slot];
        _events.ClearReceivedCalls();
    }

    async Task Because() => _after = await _roster.IsActiveAdministrator(_first);

    [Fact] void should_deny_before_and_after_revision() => (_before || _after).ShouldBeFalse();
    [Fact] void should_replay_even_with_the_same_tail() => _events.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "GetRange").ShouldBeTrue();
}
