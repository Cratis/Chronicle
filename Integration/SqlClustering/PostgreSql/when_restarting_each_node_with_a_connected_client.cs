// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.PostgreSql;

public class when_restarting_each_node_with_a_connected_client() : SqlClusterSpecification(true, ClusterChange.Restart, verifyReconnect: true)
{
#pragma warning disable xUnit1004
    [Fact(Skip = "Flaky: misses the 15 s delivery deadline after a node restart, also on main - https://github.com/Cratis/Chronicle/issues/5603")]
#pragma warning restore xUnit1004
    public void should_append_and_deliver_to_reactor_and_reducer_while_each_node_is_stopped_and_after_restart() =>
        VerifyClientAndObserversKeptProgressingThroughEveryTransition();
}
