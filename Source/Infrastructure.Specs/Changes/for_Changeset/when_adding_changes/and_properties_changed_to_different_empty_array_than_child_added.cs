// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_properties_changed_to_different_empty_array_than_child_added : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _tagsProperty;
    PropertiesChanged<ExpandoObject> _propertiesChanged;
    ChildAdded _childAdded;

    void Establish()
    {
        // Create the array property paths using AddArrayIndex to match runtime behavior
        // At runtime, ChildrenPropertyPath is created with AddArrayIndex, resulting in format "[propertyName]"
        _itemsProperty = PropertyPath.Root.AddArrayIndex("items");
        _tagsProperty = PropertyPath.Root.AddArrayIndex("tags");

        // Create a PropertiesChanged that sets tags to empty array
        var emptyArray = Array.Empty<object>();
        var propertyDifference = new PropertyDifference(
            _tagsProperty,
            emptyArray,
            emptyArray,
            ArrayIndexers.NoIndexers);

        _propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [propertyDifference]);

        _changeset.Add(_propertiesChanged);

        // Create a ChildAdded for a different array (items)
        _childAdded = new ChildAdded(
            "test-value",
            _itemsProperty,
            PropertyPath.Root,
            "child-1",
            ArrayIndexers.NoIndexers);
    }

    void Because() => _changeset.Add(_childAdded);

    [Fact] void should_keep_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldContainOnly(_propertiesChanged);
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
}
