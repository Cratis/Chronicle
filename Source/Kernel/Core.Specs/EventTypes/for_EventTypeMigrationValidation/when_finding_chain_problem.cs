// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeMigrationValidation;

public class when_finding_chain_problem : Specification
{
    static EventTypeMigrationDefinition Migration(uint from, uint to) => new() { FromGeneration = from, ToGeneration = to };

    [Fact] void should_find_nothing_for_first_generation() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 1, []).ShouldBeNull();

    [Fact] void should_report_missing_migrators_when_there_are_none() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 2, []).ShouldBeOfExactType<MissingEventTypeMigrators>();

    [Fact] void should_ignore_migrations_with_same_from_and_to() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 2, [Migration(1, 1)]).ShouldBeOfExactType<MissingEventTypeMigrators>();

    [Fact] void should_report_missing_first_generation() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 3, [Migration(2, 3)]).ShouldBeOfExactType<MissingFirstGenerationForEventType>();

    [Fact] void should_report_the_generation_with_a_gap() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 4, [Migration(1, 2), Migration(3, 4)])
            .ShouldBeOfExactType<MissingMigrationForEventTypeGeneration>();

    [Fact] void should_find_nothing_for_an_unbroken_chain() =>
        EventTypeMigrationValidation.FindChainProblem("some-event", 3, [Migration(1, 2), Migration(2, 3)]).ShouldBeNull();

    [Fact] void should_throw_the_problem_when_validating() =>
        Catch.Exception(() => EventTypeMigrationValidation.ValidateChain("some-event", 2, [])).ShouldBeOfExactType<MissingEventTypeMigrators>();

    [Fact] void should_not_throw_when_validating_an_intact_chain() =>
        Catch.Exception(() => EventTypeMigrationValidation.ValidateChain("some-event", 2, [Migration(1, 2)])).ShouldBeNull();
}
