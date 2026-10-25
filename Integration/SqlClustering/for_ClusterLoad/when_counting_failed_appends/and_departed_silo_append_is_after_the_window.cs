// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad.when_counting_failed_appends;

public class and_departed_silo_append_is_after_the_window : Specification
{
    bool _result;
    TimeSpan _sinceDeparture;

    void Establish() => _sinceDeparture = ClusterLoad.DepartureWindow + TimeSpan.FromSeconds(1);

    void Because() => _result = ClusterLoad.CountsAgainstBudget(true, _sinceDeparture);

    [Fact] void should_count_against_the_budget() => _result.ShouldEqual(true);
}
