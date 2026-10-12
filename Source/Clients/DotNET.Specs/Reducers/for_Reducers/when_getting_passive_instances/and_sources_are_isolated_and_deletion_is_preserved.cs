// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_sources_are_isolated_and_deletion_is_preserved : given.a_passive_reducer
{
    void Establish() => Seed(Event(4, -1, source: "two", tags: ["blue"]), Event(3, 5, tags: ["green"]), Event(2, 100, source: "two", tags: ["blue"]), Event(1, 2, tags: ["blue"]));

    Task Because() => ReadAll();

    [Fact] void should_remove_the_deleted_source() => _instances.Count().ShouldEqual(1);
    [Fact] void should_fold_only_the_surviving_sources_events() => ((Total)_instances.Single()).Amount.ShouldEqual(7);
    [Fact] void should_reduce_each_source_in_sequence_order() => ((Total)_instances.Single()).Positions.ShouldEqual("1,3,");
}
