// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions;

public class when_a_third_level_recursive_child_is_added : given.a_deep_recursive_child
{
    void Establish() => _subject.Project(_childrenPath, "id", [PropertyMappers.FromEventValueProvider("[features].[subFeatures].name", _ => "added name")]).Subscribe();

    void Because() => _subject.OnNext(_eventContext);

    [Fact] void should_keep_the_first_level_under_the_root() => Value(First, "id").ShouldEqual("first");
    [Fact] void should_keep_the_second_level_under_the_first() => Value(Second, "id").ShouldEqual("second");
    [Fact] void should_add_the_child_under_its_real_parent() => Leaves.Select(child => Value(child, "id")).ShouldEqual(new[] { "third", "sibling", "added" });
    [Fact] void should_set_the_new_child_name() => Value(Leaves.Single(child => Equals(Value(child, "id"), "added")), "name").ShouldEqual("added name");
    [Fact] void should_record_the_named_child_for_the_sink() => Value((System.Dynamic.ExpandoObject)_changeset.Changes.OfType<ChildAdded>().Single().Child, "name").ShouldEqual("added name");
}
