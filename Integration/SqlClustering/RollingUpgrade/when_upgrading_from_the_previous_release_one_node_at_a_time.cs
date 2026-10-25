// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade;

/// <summary>
/// Starts a cluster on the previous release and replaces its nodes with this build one at a time under append load,
/// the way a Kubernetes rolling update does. Silos of both releases share the cluster in between and call each
/// other's grains, which is what broke the 19.70 to 19.106 upgrade (#5272).
/// </summary>
/// <remarks>
/// The previous release comes from CRATIS_CHRONICLE_PREVIOUS_IMAGE, which CI sets to the development-slim image of
/// the latest release. Without it, the latest released development-slim image is used.
/// </remarks>
public class when_upgrading_from_the_previous_release_one_node_at_a_time() : SqlClusterSpecification(
    true,
    ClusterChange.RollingUpgrade,
    Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_PREVIOUS_IMAGE") ?? "cratis/chronicle:latest-development-slim")
{
    [Fact]
    public Task should_preserve_acknowledged_events_without_double_appends_and_observe_every_stored_event() => VerifyAcknowledgedEventsAreObserved();
}
