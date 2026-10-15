// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext.when_updating;

public class and_the_enclosing_budget_expires : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    IEnumerable<ConstraintIndexUpdateResult> _results;
    CancellationTokenSource _deadline;
    TaskCompletionSource _hung;

    void Establish()
    {
        _deadline = new();
        _hung = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstValidatorIndexUpdater.Update(1, Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            // Signal the deadline explicitly, rather than sleeping to create a timeout.
            await _deadline.CancelAsync();
            await _hung.Task;
        });
    }

    async Task Because() => _results = await _context.Update(1, _deadline.Token);

    void Destroy()
    {
        _hung.TrySetResult();
        _deadline.Dispose();
    }

    [Fact] void should_report_the_still_running_attempt_as_abandoned() => _results.Single(result => result.Updater == _firstValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Abandoned);
}
