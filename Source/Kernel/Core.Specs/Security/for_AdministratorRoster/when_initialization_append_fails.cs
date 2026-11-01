// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_initialization_append_fails : given.an_initializing_administrator_roster
{
    Exception _exception;

    void Establish() => _failAppend = true;

    async Task Because() => _exception = await Catch.Exception(_roster.EnsureAdministratorPrivilege);

    [Fact] void should_propagate_the_failure_to_startup() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_retry_a_storage_failure_as_a_concurrency_conflict() => _appendCalls.ShouldEqual(1);
    [Fact] void should_not_persist_a_migration_decision() => Recorded<AdministratorPrivilegeMigrated>().ShouldBeEmpty();
    [Fact] void should_log_the_storage_cause() => Errors.Any(error => error.Contains("storage failure", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_that_startup_stops() => Errors.Any(error => error.Contains("Startup is stopped", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_tell_the_operator_to_check_storage() => Errors.Any(error => error.Contains("Check System event-log storage health and permissions", StringComparison.Ordinal)).ShouldBeTrue();
}
