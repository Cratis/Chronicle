// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_bootstrap_grants_compete : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root");
        _synchronizeNextAppends = true;
    }

    async Task Because() => await Task.WhenAll(
        _roster.Grant(_first, GrantReason.Bootstrap, "first silo"),
        CreateRoster().Grant(_first, GrantReason.Bootstrap, "second silo"));

    [Fact] void should_have_a_real_concurrency_conflict() => _conflicts.ShouldEqual(1);
    [Fact] void should_append_only_one_bootstrap_grant() => Recorded<UserGrantedAdministrator>().Count(grant => grant.UserId == _first).ShouldEqual(1);
    [Fact] void should_not_append_again_after_reloading_the_winning_grant() => _appendCalls.ShouldEqual(2);
}
