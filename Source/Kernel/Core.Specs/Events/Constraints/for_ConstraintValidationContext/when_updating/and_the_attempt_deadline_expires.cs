// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext.when_updating;

public class and_the_attempt_deadline_expires : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    IEnumerable<ConstraintIndexUpdateResult> _results;
    TaskCompletionSource _hung;

    void Establish()
    {
        _hung = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstValidatorIndexUpdater.Update(1, Arg.Any<CancellationToken>()).Returns(_hung.Task);
    }

    async Task Because() => _results = await _context.Update(1, CancellationToken.None);

    void Destroy() => _hung.TrySetResult();

    [Fact] void should_distinguish_an_abandoned_deadline_from_failure() => _results.Single(result => result.Updater == _firstValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Abandoned);
    [Fact] void should_keep_the_independent_success() => _results.Single(result => result.Updater == _secondValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Applied);
}
