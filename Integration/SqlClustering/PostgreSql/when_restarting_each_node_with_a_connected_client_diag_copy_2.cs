// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.PostgreSql;

/// <summary>
/// Diagnostic (l189/5603-diag only): a second run of the restart spec to raise the chance of the intermittent failure.
/// </summary>
public class when_restarting_each_node_with_a_connected_client_diag_copy_2() : SqlClusterSpecification(true, ClusterChange.Restart, verifyReconnect: true)
{
    [Fact]
    public void should_append_and_deliver_to_reactor_and_reducer_while_each_node_is_stopped_and_after_restart() =>
        VerifyClientAndObserversKeptProgressingThroughEveryTransition();
}
