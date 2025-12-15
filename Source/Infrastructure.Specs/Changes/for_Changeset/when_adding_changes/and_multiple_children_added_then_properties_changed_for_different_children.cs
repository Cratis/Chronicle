// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_multiple_children_added_then_properties_changed_for_different_children : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    ChildAdded _childAdded1;
    ChildAdded _childAdded2;
    ExpandoObject _child1;
    ExpandoObject _child2;
    ArrayIndexers _arrayIndexers1;
    ArrayIndexers _arrayIndexers2;
    PropertiesChanged<ExpandoObject> _propertiesChanged2;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

        // Create first ChildAdded
        _child1 = new ExpandoObject();
        var child1Dict = (IDictionary<string, object?>)_child1;
        child1Dict["id"] = "child-1";
        child1Dict["name"] = "Original Name 1";

        _arrayIndexers1 = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, PropertyPath.CreateFrom([new PropertyName("id")]), "child-1")
        ]);

        _childAdded1 = new ChildAdded(
            _child1,
            _itemsProperty,
            PropertyPath.CreateFrom([new PropertyName("id")]),
            "child-1",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded1);

        // Create second ChildAdded
        _child2 = new ExpandoObject();
        var child2Dict = (IDictionary<string, object?>)_child2;
        child2Dict["id"] = "child-2";
        child2Dict["name"] = "Original Name 2";

        _arrayIndexers2 = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, PropertyPath.CreateFrom([new PropertyName("id")]), "child-2")
        ]);

        _childAdded2 = new ChildAdded(
            _child2,
            _itemsProperty,
            PropertyPath.CreateFrom([new PropertyName("id")]),
            "child-2",
            ArrayIndexers.NoIndexers);

        _changeset.Add(_childAdded2);

        // Create PropertiesChanged for first child
        var propertyDifference1 = new PropertyDifference(
            _nameProperty,
            "Original Name 1",
            "Updated Name 1",
            _arrayIndexers1);

        var propertiesChanged1 = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [propertyDifference1]);

        _changeset.Add(propertiesChanged1);

        // Create PropertiesChanged for second child
        var propertyDifference2 = new PropertyDifference(
            _nameProperty,
            "Original Name 2",
            "Updated Name 2",
            _arrayIndexers2);

        _propertiesChanged2 = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [propertyDifference2]);
    }

    void Because() => _changeset.Add(_propertiesChanged2);

    [Fact] void should_remove_all_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
    [Fact] void should_keep_both_child_added() => _changeset.Changes.OfType<ChildAdded>().Count().ShouldEqual(2);
    [Fact] void should_have_two_changes() => _changeset.Changes.Count().ShouldEqual(2);
    [Fact] void should_apply_property_change_to_first_child()
    {
        var child1Dict = (IDictionary<string, object?>)_child1;
        child1Dict["name"].ShouldEqual("Updated Name 1");
    }
    [Fact] void should_apply_property_change_to_second_child()
    {
        var child2Dict = (IDictionary<string, object?>)_child2;
        child2Dict["name"].ShouldEqual("Updated Name 2");
    }
}
