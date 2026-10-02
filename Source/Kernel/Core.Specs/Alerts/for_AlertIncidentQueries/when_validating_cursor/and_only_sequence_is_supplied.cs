// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_validating_cursor;

public class and_only_sequence_is_supplied : given.scoped_queries
{
    async Task Because() => await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, afterRaisedSequenceNumber: EventSequenceNumber.Unavailable);

    [Fact] async Task should_ignore_the_sequence_without_an_identity() => await _incidents.Received(1).GetOpenPage(new(new("affected", null), null, null, null), null, 100);
}
