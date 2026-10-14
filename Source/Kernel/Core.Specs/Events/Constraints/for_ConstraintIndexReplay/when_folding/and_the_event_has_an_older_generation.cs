// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_event_has_an_older_generation : given.a_replay
{
    void Establish()
    {
        Event(0, "old-source", "Claimed", "old-value", generation: 1);
        Event(1, "new-source", "Claimed", "new-value", generation: 3);
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_request_one_filter_per_event_type_id() => ((IEnumerable<EventType>)_sequence.ReceivedCalls().Single().GetArguments()[5]!).GroupBy(type => type.Id).ShouldEachConformTo(group => group.Count() == 1);
    [Fact] async Task should_fold_the_older_generation() => (await Values("old-source")).ShouldContainOnly(Hash("old-value").Value);
    [Fact] async Task should_fold_the_later_generation_through_the_same_id_filter() => (await Values("new-source")).ShouldContainOnly(Hash("new-value").Value);
}
