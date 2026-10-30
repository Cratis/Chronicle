// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using NSubstitute.Extensions;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_initialization_exhausts_concurrency_retries : given.an_initializing_administrator_roster
{
    Exception _exception;
    int _attempts;

    void Establish() => _eventLog.Configure().AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
        .Returns(_ =>
        {
            _attempts++;
            return Task.FromResult(AppendManyResult.Failed(CorrelationId.New(),
                [new ConcurrencyViolation(Roster.MigrationEventSourceId, EventSequenceNumber.BeforeFirst, 1)]));
        });

    async Task Because() => _exception = await Catch.Exception(_roster.EnsureAdministratorPrivilege);

    [Fact] void should_stop_after_eight_attempts() => _attempts.ShouldEqual(8);
    [Fact] void should_propagate_the_failure_to_startup() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_persist_a_migration_decision() => Recorded<AdministratorPrivilegeMigrated>().ShouldBeEmpty();
    [Fact] void should_log_the_concurrency_cause() => Errors.Single().ShouldContain("The administrator roster changed repeatedly");
    [Fact] void should_tell_the_operator_to_stop_competing_writers() => Errors.Single().ShouldContain("stop competing roster writers");
    [Fact] void should_tell_the_operator_to_restart() => Errors.Single().ShouldContain("then restart");
}
