// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.PostgreSql;

public class when_a_node_is_killed() : SqlClusterSpecification(true, ClusterChange.Kill)
{
    [Fact(Skip = "Observers stall in catch-up after a node loss until the watchdog runs: https://github.com/Cratis/Chronicle/issues/5246")]
    public Task should_preserve_acknowledged_events_without_double_appends_and_observe_every_stored_event() => VerifyAcknowledgedEventsAreObserved();
}
