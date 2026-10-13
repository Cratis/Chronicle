// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_tags_only_label_the_observer : given.a_passive_reducer
{
    protected override Type ReducerType => typeof(Labelled);

    void Establish() => Seed(Event(1, 3), Event(2, 4, tags: ["red"]));

    Task Because() => ReadById();

    [Fact] void should_not_use_descriptive_tags_as_filters() => _instance!.Amount.ShouldEqual(7);
}
