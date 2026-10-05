// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_validating_cursor;

public class and_a_sentinel_is_supplied : given.scoped_queries
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, afterRaisedSequenceNumber: EventSequenceNumber.Unavailable, afterIncidentId: _id));

    [Fact] void should_reject_instead_of_restarting_paging() => _error.ShouldBeOfExactType<InvalidAlertIncidentQuery>();
    [Fact] async Task should_not_read_storage() => await _incidents.DidNotReceive().GetOpenPage(Arg.Any<AlertIncidentFilter>(), Arg.Any<AlertIncidentCursor?>(), Arg.Any<int>());
}
