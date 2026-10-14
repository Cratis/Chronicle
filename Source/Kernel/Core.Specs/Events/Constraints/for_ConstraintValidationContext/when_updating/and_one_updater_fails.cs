// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext.when_updating;

public class and_one_updater_fails : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    IEnumerable<ConstraintIndexUpdateResult> _results;

    void Establish() => _firstValidatorIndexUpdater.Update(1, Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("failed write")));

    async Task Because() => _results = await _context.Update(1, CancellationToken.None);

    [Fact] void should_report_both_updaters() => _results.Count().ShouldEqual(2);
    [Fact] void should_report_the_failed_updater() => _results.Single(result => result.Updater == _firstValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Failed);
    [Fact] void should_report_the_applied_updater() => _results.Single(result => result.Updater == _secondValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Applied);
}
