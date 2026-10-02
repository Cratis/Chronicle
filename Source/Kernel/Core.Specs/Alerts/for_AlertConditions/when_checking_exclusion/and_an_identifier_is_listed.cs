// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_checking_exclusion;

/// <summary>
/// An observer is excluded when its identifier is listed exactly, and no other observer is.
/// </summary>
public class and_an_identifier_is_listed : given.alert_conditions
{
    bool _listed;
    bool _other;
    bool _differentCase;

    void Establish() => Configure(new AlertsOptions { ExcludeObservers = ["dev.orders"] });

    void Because()
    {
        _listed = _conditions.IsExcluded("dev.orders");
        _other = _conditions.IsExcluded("dev.orders.archive");
        _differentCase = _conditions.IsExcluded("DEV.ORDERS");
    }

    [Fact] void should_exclude_the_listed_observer() => _listed.ShouldBeTrue();
    [Fact] void should_not_exclude_another_observer() => _other.ShouldBeFalse();
    [Fact] void should_not_ignore_case() => _differentCase.ShouldBeFalse();
}
