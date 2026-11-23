// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions;

public class when_a_third_level_recursive_child_is_removed : given.a_deep_recursive_child
{
    void Establish()
    {
        Target("third");
        _subject.RemoveChild(_childrenPath, "id").Subscribe();
    }

    void Because() => _subject.OnNext(_eventContext);

    [Fact] void should_remove_only_the_requested_child() => Leaves.Select(child => Value(child, "id")).ShouldEqual(new[] { "sibling" });
    [Fact] void should_preserve_the_sibling_name() => Value(Leaves.Single(), "name").ShouldEqual("sibling");
    [Fact] void should_record_the_removal_for_the_sink() => _changeset.Changes.OfType<ChildRemoved>().Single().Key.ShouldEqual("third");
}
