// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_validating_cursor;

public class and_only_identity_is_supplied : given.scoped_queries
{
    async Task Because() => await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, afterIncidentId: _id);

    [Fact] async Task should_continue_from_position_zero() => await _incidents.Received(1).GetOpenPage(new(new("affected", null), null, null, null), new(0UL, _id), 100);
}
