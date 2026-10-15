// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_reactivating;

public class should_publish_on_first_refresh : given.an_owner
{
    AlertIncidentsMetricsOwner _next;

    async Task Establish()
    {
        await _owner.RefreshAsync();
        _gauge.ClearReceivedCalls();
        _next = CreateOwner();
    }

    async Task Because() => await _next.RefreshAsync();

    [Fact] void should_publish_for_the_new_activation() => _gauge.Received(1).Publish(_next, Arg.Any<AlertIncidentsGaugeSnapshot>());
}
