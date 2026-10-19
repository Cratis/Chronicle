// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_with_a_constraint_violation : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;

    void Establish() => _harness.AppendReturns(AppendManyResult.Failed(
        _harness.CorrelationId,
        [new ConstraintViolation("event", 1, ConstraintType.Unique, "unique", "duplicate", new ConstraintViolationDetails())]));

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        _result = await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_be_failed() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Failed);
    [Fact] void should_have_constraint_violations() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_have_conflicts() => _result.HasConflicts.ShouldBeFalse();
}
