// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.PostgreSql;

public class when_restarting_each_node_with_a_connected_client() : SqlClusterSpecification(true, ClusterChange.Restart, verifyReconnect: true)
{
    [Fact]
    public void should_append_and_deliver_to_reactor_and_reducer_while_each_node_is_stopped_and_after_restart() =>
        VerifyClientAndObserversKeptProgressingThroughEveryTransition();
}
