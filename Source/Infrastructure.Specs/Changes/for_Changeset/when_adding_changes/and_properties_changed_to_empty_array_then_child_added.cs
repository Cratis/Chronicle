// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_properties_changed_to_empty_array_then_child_added : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyDifference _propertyDifference;
    ChildAdded _childAdded;

    void Establish()
    {
        // Create the array property path using AddArrayIndex to match runtime behavior
        // At runtime, ChildrenPropertyPath is created with AddArrayIndex, resulting in format "[items]"
        _itemsProperty = PropertyPath.Root.AddArrayIndex("items");

        // Create a PropertiesChanged that sets items to empty array
        var emptyArray = Array.Empty<object>();
        _propertyDifference = new PropertyDifference(
            _itemsProperty,
            emptyArray,
            emptyArray,
            ArrayIndexers.NoIndexers);

        var propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [_propertyDifference]);

        _changeset.Add(propertiesChanged);

        // Create a ChildAdded for the same array
        _childAdded = new ChildAdded(
            "test-value",
            _itemsProperty,
            PropertyPath.Root,
            "child-1",
            ArrayIndexers.NoIndexers);
    }

    void Because() => _changeset.Add(_childAdded);

    [Fact] void should_remove_properties_changed_for_empty_array() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_one_change() => _changeset.Changes.Count().ShouldEqual(1);
}
