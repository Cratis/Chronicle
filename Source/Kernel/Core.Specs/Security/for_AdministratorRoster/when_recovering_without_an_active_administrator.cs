// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_recovering_without_an_active_administrator : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "operator");
        ConfigureRecovery("operator", "single-use-recovery");
    }

    async Task Because() => await Task.WhenAll(_roster.EnsureAdministratorPrivilege(), CreateRoster().EnsureAdministratorPrivilege());

    [Fact] void should_grant_only_once() => Recorded<UserGrantedAdministrator>().Count().ShouldEqual(1);
    [Fact] void should_record_operator_recovery() => Recorded<UserGrantedAdministrator>().Single().Reason.ShouldEqual(GrantReason.OperatorRecovery);
    [Fact] void should_record_the_nonce() => Recorded<UserGrantedAdministrator>().Single().Evidence.ShouldContain("single-use-recovery");
    [Fact] void should_record_the_host() => Recorded<UserGrantedAdministrator>().Single().Evidence.ShouldContain(Environment.MachineName);
    [Fact] void should_record_the_configuration_source() => Recorded<UserGrantedAdministrator>().Single().Evidence.ShouldContain("ConfigSource");
    [Fact] void should_record_the_force_flag() => Recorded<UserGrantedAdministrator>().Single().Evidence.ShouldContain("\"Force\":false");
}
