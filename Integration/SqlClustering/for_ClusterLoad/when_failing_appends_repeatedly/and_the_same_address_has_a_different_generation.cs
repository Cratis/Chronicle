// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad.when_failing_appends_repeatedly;

public class and_the_same_address_has_a_different_generation : given.a_load_with_a_departed_silo
{
    void Establish()
    {
        _clock.Advance(TimeSpan.FromSeconds(3));
        _appendFailure = ConnectionFailure(_departed.Generation + 1);
    }

    void Because() => _error = Catch.Exception(() => FailAppends(40));

    [Fact] void should_exhaust_the_budget() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_report_the_exhausted_budget() => _error.Message.ShouldEqual("More than 30 append attempts failed during 1 membership transition(s).");
    [Fact] void should_allow_thirty_failures_before_throwing() => _attempts.ShouldEqual(31);
}
