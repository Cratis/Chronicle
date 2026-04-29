// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_clearing_nested;

public class and_nested_property_is_cleared : for_Changeset.when_adding_changes.given.a_changeset
{
    PropertyPath _nestedProperty;

    void Establish() => _nestedProperty = new PropertyPath("command");

    void Because() => _changeset.ClearNested(_nestedProperty);

    [Fact] void should_have_changes() => _changeset.HasChanges.ShouldBeTrue();
    [Fact] void should_have_one_change() => _changeset.Changes.Count().ShouldEqual(1);
    [Fact] void should_have_a_nested_cleared_change() => _changeset.Changes.ShouldContain(_ => _ is NestedCleared);

    [Fact]
    void should_have_correct_nested_property_path()
    {
        var change = _changeset.Changes.OfType<NestedCleared>().Single();
        change.NestedProperty.ShouldEqual(_nestedProperty);
    }
}
