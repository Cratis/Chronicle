// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_child_added_then_properties_changed_to_same_non_empty_array : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    PropertyDifference _namePropertyDifference;
    ChildAdded _childAdded;

    void Establish()
    {
        _itemsProperty = PropertyPath.Root.AddArrayIndex("items");
        _nameProperty = new PropertyPath("name");

        var child = new ExpandoObject();
        _childAdded = new ChildAdded(
            child,
            _itemsProperty,
            PropertyPath.Root,
            "child-1",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded);

        var itemsPropertyDifference = new PropertyDifference(
            new PropertyPath("items"),
            Array.Empty<object>(),
            new[] { child },
            ArrayIndexers.NoIndexers);

        _namePropertyDifference = new PropertyDifference(
            _nameProperty,
            "Old Name",
            "New Name",
            ArrayIndexers.NoIndexers);

        var propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [itemsPropertyDifference, _namePropertyDifference]);

        _changeset.Add(propertiesChanged);
    }

    [Fact] void should_keep_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Count().ShouldEqual(1);
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
    [Fact]
    void should_only_keep_non_conflicting_property_in_properties_changed()
    {
        var propertiesChanged = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().First();
        propertiesChanged.Differences.ShouldContainOnly(_namePropertyDifference);
    }
}
