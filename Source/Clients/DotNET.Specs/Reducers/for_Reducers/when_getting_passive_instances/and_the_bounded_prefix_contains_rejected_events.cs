// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_the_bounded_prefix_contains_rejected_events : given.a_passive_reducer
{
    void Establish() => Seed(Event(1, 100, tags: ["red"]), Event(2, 3, tags: ["blue"]), Event(3, 1000, tags: ["green"]));

    Task Because() => ReadAll((EventCount)2);

    [Fact] void should_not_read_past_the_type_selected_prefix() => ((Total)_instances.Single()).Amount.ShouldEqual(3);
    [Fact] void should_keep_original_event_positions() => ((Total)_instances.Single()).Positions.ShouldEqual("2,");
}
