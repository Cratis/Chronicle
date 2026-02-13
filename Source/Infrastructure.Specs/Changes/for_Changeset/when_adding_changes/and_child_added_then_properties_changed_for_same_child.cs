// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_child_added_then_properties_changed_for_same_child : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    ChildAdded _childAdded;
    ExpandoObject _child;
    ArrayIndexers _arrayIndexers;
    PropertiesChanged<ExpandoObject> _propertiesChanged;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

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

        // Create a PropertiesChanged that targets the same child
        var propertyDifference = new PropertyDifference(
            _nameProperty,
            "Original Name",
            "Updated Name",
            _arrayIndexers);

        _propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [propertyDifference]);
    }

    void Because() => _changeset.Add(_propertiesChanged);

    [Fact] void should_remove_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
    [Fact] void should_keep_child_added() => _changeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_one_change() => _changeset.Changes.Count().ShouldEqual(1);
    [Fact]
    void should_apply_property_change_to_child()
    {
        var childDict = (IDictionary<string, object?>)_child;
        childDict["name"].ShouldEqual("Updated Name");
    }
}
