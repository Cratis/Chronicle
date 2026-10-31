// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_categories_are_combined : given.a_passive_reducer
{
    protected override Type ReducerType => typeof(Combined);

    void Establish() => Seed(Event(1, 3, tags: ["blue"]), Event(2, 100, sourceType: "Other", tags: ["blue"]), Event(3, 100, streamType: "Other", tags: ["blue"]), Event(4, 100, tags: ["green"]));

    Task Because() => ReadById();

    [Fact] void should_require_every_category_to_match() => _instance!.Amount.ShouldEqual(3);
}
