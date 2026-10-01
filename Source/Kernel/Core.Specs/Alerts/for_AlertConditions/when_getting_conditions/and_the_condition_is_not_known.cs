// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_getting_conditions;

/// <summary>
/// Asking for a condition the kernel does not know is a programming error and says so, instead of answering with
/// settings that look real.
/// </summary>
public class and_the_condition_is_not_known : given.alert_conditions
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _conditions.For("made-up"));

    [Fact] void should_throw_unknown_alert_condition() => _error.ShouldBeOfExactType<UnknownAlertCondition>();
}
