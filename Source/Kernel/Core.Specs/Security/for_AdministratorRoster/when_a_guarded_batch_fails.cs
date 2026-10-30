// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_guarded_batch_fails : given.an_administrator_roster
{
    Exception _exception;

    async Task Establish()
    {
        await AddUser(_first, "first");
        await AddUser(_second, "second");
        SeedGrant(_first);
        SeedGrant(_second);
        _failAppend = true;
    }

    async Task Because() => _exception = await Catch.Exception(() => _roster.RemoveUser(_first));

    [Fact] void should_report_the_failure() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_record_a_partial_revocation() => Recorded<UserRevokedAdministrator>().ShouldBeEmpty();
    [Fact] void should_not_record_a_partial_removal() => Recorded<UserRemoved>().ShouldBeEmpty();
}
