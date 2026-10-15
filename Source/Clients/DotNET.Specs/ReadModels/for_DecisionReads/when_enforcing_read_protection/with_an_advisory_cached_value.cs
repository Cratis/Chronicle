// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_an_advisory_cached_value : given.a_protected_reader
{
    Exception _error;

    void Because()
    {
        using var frame = _unit.EnforceReadProtection();
        var cached = DecisionRead<Model>.Unprotected("source", new Model("source"));
        using var advisory = _unit.AllowReads(ReadAllowance.Advisory, "nested Off command");
        _error = Catch.Exception(() => _unit.AddDecisionRead(cached));
    }

    [Fact] void should_not_promote_the_advisory_value_into_a_dependency() => _error.ShouldBeOfExactType<UnprotectedDecisionRead>();
    [Fact] void should_not_fold_a_cached_advisory_value() => _folds.ShouldEqual(0);
}
