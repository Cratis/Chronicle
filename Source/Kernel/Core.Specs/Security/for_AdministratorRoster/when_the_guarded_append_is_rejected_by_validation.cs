// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_the_guarded_append_is_rejected_by_validation : given.an_initializing_administrator_roster
{
    Exception _exception;

    void Establish() => _rejectWithViolations = true;

    async Task Because() => _exception = await Catch.Exception(_roster.EnsureAdministratorPrivilege);

    [Fact] void should_stop_startup() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_retry_validation_as_a_concurrency_conflict() => _appendCalls.ShouldEqual(1);
    [Fact] void should_not_persist_a_migration_decision() => Recorded<AdministratorPrivilegeMigrated>().ShouldBeEmpty();
    [Fact] void should_expose_the_violation_message() => Errors.Any(error => error.Contains("The migration content violates its schema", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_the_schema_constraint() => Errors.Any(error => error.Contains("SchemaValidation", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_the_schema_path() => Errors.Any(error => error.Contains("#/userId", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_the_rejected_event_type() => Errors.Any(error => error.Contains(typeof(AdministratorPrivilegeMigrated).GetEventType().Id.Value, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_skipped_concurrency_check() => Errors.Any(error => error.Contains("Concurrency check performed: False", StringComparison.Ordinal)).ShouldBeTrue();
}
