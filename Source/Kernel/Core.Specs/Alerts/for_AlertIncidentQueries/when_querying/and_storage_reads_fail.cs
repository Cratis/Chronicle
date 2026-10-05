// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_storage_reads_fail : given.scoped_queries
{
    Exception? _error;

    void Establish() => _incidents.GetOpenCounts(Arg.Any<AlertIncidentScope>())
        .Returns(Task.FromException<IEnumerable<AlertIncidentCount>>(new InvalidAlertIncidentQuery("Storage unavailable.")));
    async Task Because() => _error = await Catch.Exception(() => AlertIncidentSummary.GetOpenIncidentCounts("affected", _storage, _readiness));

    [Fact] void should_not_turn_storage_failures_into_zero_counts() => _error.ShouldBeOfExactType<InvalidAlertIncidentQuery>();
}
