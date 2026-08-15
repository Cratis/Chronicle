// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyOptions;

/// <summary>
/// Opting in is a single assignment on the options a consumer already reaches through
/// <c>ChronicleOptions.ConcurrencyOptions</c>, and it does not disturb anything else on them.
/// </summary>
public class when_opting_into_the_first_append_check : Specification
{
    ConcurrencyOptions _options;

    void Establish() => _options = new ConcurrencyOptions();

    void Because() => _options.CheckFirstAppendIntoAScope = true;

    [Fact] void should_check_the_first_append_into_a_scope() => _options.CheckFirstAppendIntoAScope.ShouldBeTrue();
    [Fact] void should_leave_the_strategy_alone() => _options.DefaultStrategy.ShouldEqual(typeof(OptimisticConcurrencyStrategy));
}
