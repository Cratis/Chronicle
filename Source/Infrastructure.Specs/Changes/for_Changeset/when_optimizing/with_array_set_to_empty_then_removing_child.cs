// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_optimizing;

public class with_array_set_to_empty_then_removing_child : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyDifference _propertyDifference;
    ChildRemoved _childRemoved;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");

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

        // Create a ChildRemoved for the same array
        _childRemoved = new ChildRemoved(
            "test-value",
            _itemsProperty,
            PropertyPath.Root,
            "child-1");

        _changeset.Add(_childRemoved);
    }

    void Because() => _changeset.Optimize();

    [Fact] void should_remove_properties_changed_for_empty_array() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
    [Fact] void should_keep_child_removed() => _changeset.Changes.OfType<ChildRemoved>().ShouldContainOnly(_childRemoved);
    [Fact] void should_have_one_change() => _changeset.Changes.Count().ShouldEqual(1);
}
