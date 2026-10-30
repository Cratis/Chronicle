// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_checking_an_unchanged_roster : given.an_administrator_roster
{
    bool _firstCheck;
    bool _secondCheck;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        await _roster.IsActiveAdministrator(_first);
        _events.ClearReceivedCalls();
    }

    async Task Because()
    {
        _firstCheck = await _roster.IsActiveAdministrator(_first);
        _secondCheck = await _roster.IsActiveAdministrator(_first);
    }

    [Fact] void should_allow_both_checks() => (_firstCheck && _secondCheck).ShouldBeTrue();
    [Fact] void should_read_the_authoritative_tail_for_every_check() => _events.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetTailSequenceNumber").ShouldEqual(2);
    [Fact] void should_not_replay_an_unchanged_tail() => _events.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "GetRange").ShouldBeFalse();
}
