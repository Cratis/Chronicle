// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_child_join_has_an_empty_initial_state : given.a_sink_with_a_child_join
{
    async Task Because() => await _sink.ApplyChanges(_key, _changeset, 42UL);

    [Fact] void should_update_the_target_child() => Child("root")["name"].ShouldEqual("after");
    [Fact] void should_preserve_the_root_scalar() => Root("root")["title"].ShouldEqual("root title");
    [Fact] void should_preserve_the_initialization_marker() => Root("root")["__initialized"].ShouldEqual(true);
    [Fact] void should_preserve_the_other_child_fields() => Child("root")["extra"].ShouldEqual("preserved");
    [Fact] void should_preserve_both_children() => Children("root").Length.ShouldEqual(2);
    [Fact] void should_preserve_the_sibling() => Children("root")[1]["name"].ShouldEqual("sibling name");
}
