// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_validating_cursor;

public class and_identity_is_empty : given.scoped_queries
{
    async Task Because() => await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, afterRaisedSequenceNumber: EventSequenceNumber.Unavailable, afterIncidentId: new IncidentId(Guid.Empty));

    [Fact] async Task should_start_at_the_beginning() => await _incidents.Received(1).GetOpenPage(new(new("affected", null), null, null, null), null, 100);
}
