// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyOptions;

/// <summary>
/// <para>
/// The second half of the semver guard, next to
/// <c>for_OptimisticConcurrencyStrategy/when_getting_a_scope/and_nothing_matches_the_narrowing</c>. That one pins
/// the behavior; this pins the value the behavior comes from, so flipping the default cannot happen by accident -
/// it takes changing a constant a spec names out loud.
/// </para>
/// <para>
/// Changing either of these is a major release. The constant is scheduled to become true in the next one.
/// </para>
/// </summary>
public class when_nothing_configures_them : Specification
{
    ConcurrencyOptions _options;

    void Establish() => _options = new ConcurrencyOptions();

    [Fact] void should_not_check_the_first_append_into_a_scope() => _options.CheckFirstAppendIntoAScope.ShouldBeFalse();
    [Fact] void should_take_that_from_the_declared_default() => ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault.ShouldBeFalse();
    [Fact] void should_use_the_optimistic_strategy() => _options.DefaultStrategy.ShouldEqual(typeof(OptimisticConcurrencyStrategy));
}
