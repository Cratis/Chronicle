// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Mining a scope whose established patterns could not be read would count from a fresh sketch and wipe what was
/// established on the next flush. Failing the batch instead redelivers it later - nothing was mined yet, so the
/// retry counts nothing twice.
/// </summary>
public class and_restoring_established_patterns_fails : given.a_pattern_miner
{
    static readonly PatternGroupingKey _scope = "user-42";

    Exception _error;

    void Establish() => _patterns.GetForScope(_scope).Returns<IEnumerable<BehaviorPattern>>(_ => throw new Exception("storage broke"));

    async Task Because() => _error = await Catch.Exception(async () => await _miner.Mine([Features(_scope, "ApproveExpenseReport")]));

    [Fact] void should_fail_the_batch() => _error.ShouldNotBeNull();
    [Fact] void should_carry_the_failure() => _error.Message.ShouldEqual("storage broke");
    [Fact] async Task should_count_nothing() => (await _miner.GetSurvivingPatterns(_scope)).ShouldBeEmpty();
}
