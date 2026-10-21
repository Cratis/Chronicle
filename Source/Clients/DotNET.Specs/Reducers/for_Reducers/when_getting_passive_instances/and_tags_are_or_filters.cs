// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_tags_are_or_filters : given.a_passive_reducer
{
    void Establish() => Seed(Event(3, 4, tags: ["green"]), Event(1, 2, tags: ["blue"]), Event(2, 100, tags: ["red"]));

    Task Because() => ReadById();

    [Fact] void should_fold_either_matching_tag() => _instance!.Amount.ShouldEqual(6);
    [Fact] void should_keep_sequence_order_after_rejection() => _instance!.Positions.ShouldEqual("1,3,");
}
