// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_the_limit_is_negative : given.scoped_queries
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, limit: -1));

    [Fact] void should_reject_the_limit() => _error.ShouldBeOfExactType<InvalidAlertIncidentQuery>();
    [Fact] async Task should_not_read_storage() => await _incidents.DidNotReceive().GetOpenPage(Arg.Any<AlertIncidentFilter>(), Arg.Any<AlertIncidentCursor?>(), Arg.Any<int>());
}
