// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_no_filter_is_declared : given.a_passive_reducer
{
    protected override Type ReducerType => typeof(Unfiltered);

    void Establish() => Seed(Event(1, 3, sourceType: "Other", streamType: "Other"), Event(2, 4, tags: ["red"]));

    Task Because() => ReadById();

    [Fact] void should_treat_unspecified_source_and_all_streams_as_wildcards() => _instance!.Amount.ShouldEqual(7);
}
