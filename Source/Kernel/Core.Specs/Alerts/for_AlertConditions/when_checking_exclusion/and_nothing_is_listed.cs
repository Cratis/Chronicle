// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_checking_exclusion;

/// <summary>
/// With no exclusions configured no observer is excluded.
/// </summary>
public class and_nothing_is_listed : given.alert_conditions
{
    bool _excluded;

    void Because() => _excluded = _conditions.IsExcluded("anything");

    [Fact] void should_not_exclude_the_observer() => _excluded.ShouldBeFalse();
}
