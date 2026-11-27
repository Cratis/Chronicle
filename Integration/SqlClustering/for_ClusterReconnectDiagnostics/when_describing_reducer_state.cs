// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterReconnectDiagnostics;

public class when_describing_reducer_state : Specification
{
    string _description;

    void Because() => _description = ClusterReconnectDiagnostics.DescribeState("reconnect", new ReducerState(ObserverRunningState.Disconnected, true, 42, 41, 50));

    [Fact] void should_name_the_observer() => _description.ShouldContain("Observer reconnect");
    [Fact] void should_include_the_running_state() => _description.ShouldContain("RunningState=Disconnected");
    [Fact] void should_include_subscription_status() => _description.ShouldContain("IsSubscribed=True");
    [Fact] void should_include_the_next_position() => _description.ShouldContain("NextEventSequenceNumber=42");
    [Fact] void should_include_the_last_handled_position() => _description.ShouldContain("LastHandledEventSequenceNumber=41");
    [Fact] void should_include_the_tail() => _description.ShouldContain("TailEventSequenceNumber=50");
}
