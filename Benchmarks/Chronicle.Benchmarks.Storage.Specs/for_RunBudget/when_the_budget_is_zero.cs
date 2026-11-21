// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunBudget;

public class when_the_budget_is_zero : Specification
{
    RunBudget _budget = default!;
    Exception? _error;
    void Establish() => _budget = new(TimeSpan.Zero);
    void Because() => _error = Catch.Exception(_budget.ThrowIfExpired);
    void Destroy() => _budget.Dispose();
    [Fact] void should_be_expired() => _budget.Expired.ShouldBeTrue();
    [Fact] void should_cancel_the_token() => _budget.Token.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_throw_a_budget_exception() => _error.ShouldBeOfExactType<BudgetExceededException>();
}
