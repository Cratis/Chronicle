// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad.when_failing_appends_repeatedly;

public class and_the_departed_incarnation_is_within_the_window : given.a_load_with_a_departed_silo
{
    void Establish() => _clock.Advance(TimeSpan.FromSeconds(3));

    void Because() => _error = Catch.Exception(() =>
    {
        FailAppends(40);
        _clock.Advance(ClusterLoad.DepartureWindow - TimeSpan.FromSeconds(3));
        FailAppends(1);
    });

    [Fact] void should_not_exhaust_the_budget() => _error.ShouldBeNull();
    [Fact] void should_continue_after_more_than_thirty_departed_incarnation_failures() => _attempts.ShouldEqual(41);
}
