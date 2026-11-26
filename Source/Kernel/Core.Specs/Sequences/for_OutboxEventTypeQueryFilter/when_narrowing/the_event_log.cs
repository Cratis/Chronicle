// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_event_log : given.a_mixed_catalog
{
    async Task Because() => await Narrow("event-log");

    [Fact] void should_leave_the_criteria_unchanged() => _result.ShouldEqual(_criteria);
    [Fact] void should_not_read_the_catalog() => _eventStore.EventTypes.DidNotReceive().GetLatestForAllEventTypes();
}
