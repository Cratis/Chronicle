// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_two_parents_hold_the_joined_child : given.a_sink_with_a_child_join
{
    void Establish()
    {
        StoreRoot("second-root");
        _key = new Key("join-source", ArrayIndexers.NoIndexers);
    }

    async Task Because() => await _sink.ApplyChanges(_key, _changeset, 42UL);

    [Fact] void should_update_the_first_parent() => Child("root")["name"].ShouldEqual("after");
    [Fact] void should_update_the_second_parent() => Child("second-root")["name"].ShouldEqual("after");
    [Fact] void should_preserve_the_first_parent_scalar() => Root("root")["title"].ShouldEqual("root title");
    [Fact] void should_preserve_the_second_parent_scalar() => Root("second-root")["title"].ShouldEqual("root title");
    [Fact] void should_not_create_a_phantom_root() => _sink.Collection.ContainsKey("join-source").ShouldBeFalse();
}
