// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_the_filter_uses_a_registered_source : given.a_passive_reducer
{
    protected override Type ReducerType => typeof(Typed);

    void Establish() => Seed(Event(1, 3, tags: ["blue"]), Event(2, 100, sourceType: "Other", tags: ["blue"]), Event(3, 100, streamType: "Other", tags: ["blue"]), Event(4, 100, tags: ["red"]));

    Task Because() => ReadAll();

    [Fact] void should_resolve_source_stream_and_tag_filters_together() => ((Total)_instances.Single()).Amount.ShouldEqual(3);
}
