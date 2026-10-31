// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext.when_updating;

public class and_a_cancelled_write_throws_a_provider_error : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    CancellationTokenSource _budget;
    IEnumerable<ConstraintIndexUpdateResult> _results;

    void Establish()
    {
        _budget = new();
        _firstValidatorIndexUpdater.Update(1, Arg.Any<CancellationToken>()).Returns(call =>
        {
            // Cancel before returning an already-faulted task, so the provider error wins the WaitAsync race.
            _ = _budget.CancelAsync();
            return Task.FromException(new IOException("provider cancelled the write"));
        });
    }

    async Task Because() => _results = await _context.Update(1, _budget.Token);

    void Destroy() => _budget.Dispose();

    [Fact] void should_classify_the_interrupted_attempt_as_failed() => _results.Single(result => result.Updater == _firstValidatorIndexUpdater).Outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Failed);
}
