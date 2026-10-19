// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_with_a_decision_conflict : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;

    void Establish() => _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.Unit.AddDecisionRead(for_UnitOfWorkMiddleware.ProtectedRead.Create());
        _harness.AddEvent();
        _result = await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_report_the_decision_conflict() => _result.DecisionConflicts.ShouldContain(new DecisionConflict(typeof(object), "source"));
    [Fact] void should_also_report_the_violation_that_caused_it() => _result.ConcurrencyViolations.Select(_ => _.EventSourceId.Value).ShouldContainOnly("source");
}
