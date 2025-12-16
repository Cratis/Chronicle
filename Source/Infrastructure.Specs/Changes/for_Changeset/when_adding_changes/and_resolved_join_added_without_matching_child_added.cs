// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_resolved_join_added_without_matching_child_added : given.a_changeset_with_parent
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    ResolvedJoin _resolvedJoin;
    ArrayIndexers _arrayIndexers;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

        _arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, PropertyPath.CreateFrom([new PropertyName("id")]), "child-1")
        ]);

        // Create a ResolvedJoin without a matching ChildAdded
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

    [Fact] void should_keep_resolved_join() => _parentChangeset.Changes.OfType<ResolvedJoin>().ShouldContainOnly(_resolvedJoin);
    [Fact] void should_have_one_change() => _parentChangeset.Changes.Count().ShouldEqual(1);
}
