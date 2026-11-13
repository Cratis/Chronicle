// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_Verdict;

/// <summary>
/// Specifies that a refused retry key is not proof that its original append was absent.
/// </summary>
public class when_a_retry_key_conflicts : given.a_snapshot
{
    VerdictResult _result = null!;

    void Because() => _result = Verdict.Compare(Input with { Ledger = [new("one", "source", 7, "conflict")] });

    [Fact] void should_report_the_conflicting_intention() => _result.Violations.ShouldContain("Idempotency conflict for a durable intention.");
    [Fact] void should_not_claim_the_initial_append_was_definitively_refused() => _result.Violations.ShouldNotContain("Definitively failed append present in event log.");
    [Fact] void should_not_pass_the_run() => _result.IsClean.ShouldBeFalse();
}
