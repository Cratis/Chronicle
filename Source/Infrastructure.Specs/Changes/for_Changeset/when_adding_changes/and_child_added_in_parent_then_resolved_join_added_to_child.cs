// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_child_added_in_parent_then_resolved_join_added_to_child : given.a_changeset_with_parent
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    ChildAdded _childAdded;
    ExpandoObject _child;
    ResolvedJoin _resolvedJoin;
    ArrayIndexers _arrayIndexers;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

        // Create a child in parent changeset
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

        _parentChangeset.Add(_childAdded);

        // Create a ResolvedJoin that targets the same child
        var propertyDifference = new PropertyDifference(
            _nameProperty,
            "Original Name",
            "Updated Name",
            ArrayIndexers.NoIndexers);

        var propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _childInitialState,
            [propertyDifference]);

        var resolvedJoinChanges = new List<Change> { propertiesChanged };

        _resolvedJoin = new ResolvedJoin(
            _childInitialState,
            "child-1",
            _itemsProperty,
            _arrayIndexers,
            resolvedJoinChanges);

        _parentChangeset.Add(_resolvedJoin);
    }

    void Because() => _childChangeset.Add(new PropertiesChanged<ExpandoObject>(_childInitialState, []));

    [Fact] void should_remove_resolved_join() => _parentChangeset.Changes.OfType<ResolvedJoin>().ShouldBeEmpty();
    [Fact] void should_keep_child_added() => _parentChangeset.Changes.OfType<ChildAdded>().ShouldContainOnly(_childAdded);
    [Fact] void should_have_one_change() => _parentChangeset.Changes.Count().ShouldEqual(1);
    [Fact]
    void should_apply_property_change_to_child()
    {
        var childDict = (IDictionary<string, object?>)_child;
        childDict["name"].ShouldEqual("Updated Name");
    }
}
