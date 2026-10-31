// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.SqlServer;

public class when_restarting_nodes_one_by_one_under_load() : SqlClusterSpecification(false, ClusterChange.Restart)
{
#pragma warning disable xUnit1004
    [Fact(Skip = "Rolling restart misses acknowledged events on SQL Server - https://github.com/Cratis/Chronicle/issues/5350")]
#pragma warning restore xUnit1004
    public Task should_preserve_acknowledged_events_without_double_appends_and_observe_every_stored_event() => VerifyAcknowledgedEventsAreObserved();
}
