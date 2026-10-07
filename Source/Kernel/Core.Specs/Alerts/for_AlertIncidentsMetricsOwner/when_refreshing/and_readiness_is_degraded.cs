// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_refreshing;

public class and_readiness_is_degraded : given.an_owner
{
    void Establish() => _readiness.Get().Returns(AlertIncidentsReadinessState.Degraded);

    async Task Because() => await _owner.RefreshAsync();

    [Fact] void should_not_query_storage() => _incidents.DidNotReceive().GetOpenCountsByObserver(Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_previous_snapshot() => _gauge.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<AlertIncidentsGaugeSnapshot>());
}
