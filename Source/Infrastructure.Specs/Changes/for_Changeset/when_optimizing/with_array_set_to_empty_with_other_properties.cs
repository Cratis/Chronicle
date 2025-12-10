// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_optimizing;

public class with_array_set_to_empty_with_other_properties : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    PropertyDifference _itemsDifference;
    PropertyDifference _nameDifference;
    PropertiesChanged<ExpandoObject> _originalPropertiesChanged;
    ChildAdded _childAdded;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

        // Create a PropertiesChanged that sets items to empty array AND sets name
        var emptyArray = Array.Empty<object>();
        _itemsDifference = new PropertyDifference(
            _itemsProperty,
            emptyArray,
            emptyArray,
            ArrayIndexers.NoIndexers);

        _nameDifference = new PropertyDifference(
            _nameProperty,
            "Test Name",
            "Test Name",
            ArrayIndexers.NoIndexers);

        _originalPropertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [_itemsDifference, _nameDifference]);

        _changeset.Add(_originalPropertiesChanged);

        // Create a ChildAdded for the items array
        _childAdded = new ChildAdded(
            "test-value",
            _itemsProperty,
            PropertyPath.Root,
            "child-1",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded);
    }

    void Because() => _changeset.Optimize();

    [Fact] void should_keep_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldNotBeEmpty();
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
    [Fact] void should_only_have_name_property_difference() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().First().Differences.ShouldContainOnly(_nameDifference);
    [Fact] void should_not_have_items_property_difference() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().First().Differences.ShouldNotContain(_itemsDifference);
}
