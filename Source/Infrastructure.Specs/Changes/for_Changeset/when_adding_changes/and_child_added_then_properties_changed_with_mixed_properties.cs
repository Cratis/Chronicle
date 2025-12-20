// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_child_added_then_properties_changed_with_mixed_properties : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    PropertyPath _descriptionProperty;
    ChildAdded _childAdded;
    ExpandoObject _child;
    ArrayIndexers _arrayIndexers;
    PropertyDifference _nonChildPropertyDifference;
    PropertiesChanged<ExpandoObject> _propertiesChanged;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");
        _descriptionProperty = new PropertyPath("description");

        // Create a ChildAdded
        _child = new ExpandoObject();
        var childDict = (IDictionary<string, object?>)_child;
        childDict["id"] = "child-1";
        childDict["name"] = "Original Name";

        _arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, PropertyPath.CreateFrom([new PropertyName("id")]), "child-1")
        ]);

        _childAdded = new ChildAdded(
            _child,
            _itemsProperty,
            PropertyPath.CreateFrom([new PropertyName("id")]),
            "child-1",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded);

        // Create a PropertiesChanged with both child and non-child properties
        var childPropertyDifference = new PropertyDifference(
            _nameProperty,
            "Original Name",
            "Updated Name",
            _arrayIndexers);

        _nonChildPropertyDifference = new PropertyDifference(
            _descriptionProperty,
            "Old Description",
            "New Description",
            ArrayIndexers.NoIndexers);

        _propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [childPropertyDifference, _nonChildPropertyDifference]);
    }

    void Because() => _changeset.Add(_propertiesChanged);

    [Fact] void should_keep_properties_changed_with_non_child_property() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Count().ShouldEqual(1);
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
    [Fact] void should_apply_child_property_to_child()
    {
        var childDict = (IDictionary<string, object?>)_child;
        childDict["name"].ShouldEqual("Updated Name");
    }
    [Fact] void should_keep_non_child_property_in_properties_changed()
    {
        var propertiesChanged = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().First();
        propertiesChanged.Differences.ShouldContainOnly(_nonChildPropertyDifference);
    }
}
