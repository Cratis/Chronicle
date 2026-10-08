// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_refreshing;

public class and_readiness_is_ready : given.an_owner
{
    AlertIncidentsGaugeSnapshot _snapshot;

    void Establish() => _gauge.When(_ => _.Publish(Arg.Any<object>(), Arg.Any<AlertIncidentsGaugeSnapshot>())).Do(call => _snapshot = call.Arg<AlertIncidentsGaugeSnapshot>());

    async Task Because() => await _owner.RefreshAsync();

    [Fact] void should_sample_readiness_before_the_rows() => Received.InOrder(() =>
    {
        _readiness.Get();
        _incidents.GetOpenCountsByObserver(Arg.Any<CancellationToken>());
    });
    [Fact] void should_publish_for_itself() => _gauge.Received(1).Publish(_owner, Arg.Any<AlertIncidentsGaugeSnapshot>());
    [Fact] void should_publish_the_counts() => _snapshot.Measurements.Single().Value.ShouldEqual(2L);
}
