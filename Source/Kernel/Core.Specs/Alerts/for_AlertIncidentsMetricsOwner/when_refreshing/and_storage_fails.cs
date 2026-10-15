// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_refreshing;

public class and_storage_fails : given.an_owner
{
    Exception _exception;

    void Establish() => _incidents.GetOpenCountsByObserver(Arg.Any<CancellationToken>()).Returns<Task<IEnumerable<Storage.Alerts.AlertIncidentObserverCount>>>(_ => throw new InvalidOperationException());

    async Task Because() => _exception = await Catch.Exception(_owner.RefreshAsync);

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_previous_snapshot() => _gauge.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<AlertIncidentsGaugeSnapshot>());
}
