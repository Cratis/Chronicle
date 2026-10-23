// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_describing_observer_state : Specification
{
    string _description = null!;

    void Because() => _description = ClusterLoadDiagnostics.DescribeState(new ReactorState("cluster-integrity", ObserverRunningState.Active, true, 3, 2, 4));

    [Fact] public void should_identify_the_observer() => _description.ShouldContain("Observer cluster-integrity");
    [Fact] public void should_include_the_running_state() => _description.ShouldContain("RunningState=Active");
    [Fact] public void should_include_the_subscription_state() => _description.ShouldContain("IsSubscribed=True");
    [Fact] public void should_include_the_last_handled_sequence() => _description.ShouldContain("LastHandledEventSequenceNumber=2");
    [Fact] public void should_include_the_next_sequence() => _description.ShouldContain("NextEventSequenceNumber=3");
    [Fact] public void should_include_the_tail_sequence() => _description.ShouldContain("TailEventSequenceNumber=4");
}
