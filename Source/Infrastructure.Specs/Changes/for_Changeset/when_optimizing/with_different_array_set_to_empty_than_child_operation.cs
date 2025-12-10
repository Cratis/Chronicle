// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_optimizing;

public class with_different_array_set_to_empty_than_child_operation : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _tagsProperty;
    PropertyDifference _propertyDifference;
    PropertiesChanged<ExpandoObject> _originalPropertiesChanged;
    ChildAdded _childAdded;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _tagsProperty = new PropertyPath("tags");

        // Create a PropertiesChanged that sets tags to empty array
        var emptyArray = Array.Empty<object>();
        _propertyDifference = new PropertyDifference(
            _tagsProperty,
            emptyArray,
            emptyArray,
            ArrayIndexers.NoIndexers);

        _originalPropertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [_propertyDifference]);

        _changeset.Add(_originalPropertiesChanged);

        // Create a ChildAdded for items array (different from tags)
        _childAdded = new ChildAdded(
            "test-value",
            _itemsProperty,
            PropertyPath.Root,
            "child-1",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded);
    }

    void Because() => _changeset.Optimize();

    [Fact] void should_keep_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldContainOnly(_originalPropertiesChanged);
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
}
