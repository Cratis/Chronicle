// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

/// <summary>
/// A <see cref="ResolvedJoin"/> whose first change carries no property differences is consolidated away
/// before the joined values arrive - they must still reach the child once they do.
/// </summary>
public class and_resolved_join_is_consolidated_before_carrying_any_properties : given.a_changeset_with_parent
{
    PropertyPath _itemsProperty;
    PropertyPath _identifiedByProperty;
    ChildAdded _childAdded;
    ExpandoObject _child;
    IChangeset<object, ExpandoObject> _joinChangeset;
    ArrayIndexers _arrayIndexers;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _identifiedByProperty = PropertyPath.CreateFrom([new PropertyName("id")]);

        _child = new ExpandoObject();
        var childDict = (IDictionary<string, object?>)_child;
        childDict["id"] = "child-1";
        childDict["name"] = "Original Name";

        _arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, _identifiedByProperty, "child-1")
        ]);

        _childAdded = new ChildAdded(
            _child,
            _itemsProperty,
            _identifiedByProperty,
            "child-1",
            ArrayIndexers.NoIndexers);

        _parentChangeset.Add(_childAdded);

        _joinChangeset = _parentChangeset.ResolvedJoin(_itemsProperty, "child-1", _incoming, _arrayIndexers);

        // Mirrors the projection pipeline, which adds the child before setting the joined properties.
        _joinChangeset.Add(new ChildAdded(
            new ExpandoObject(),
            _itemsProperty,
            _identifiedByProperty,
            "child-1",
            _arrayIndexers));
    }

    void Because() => _joinChangeset.Add(new PropertiesChanged<ExpandoObject>(
        _childInitialState,
        [new PropertyDifference(new PropertyPath("name"), "Original Name", "Updated Name", ArrayIndexers.NoIndexers)]));

    [Fact] void should_apply_the_property_change_to_the_child() => ((IDictionary<string, object?>)_child)["name"].ShouldEqual("Updated Name");
    [Fact] void should_remove_resolved_join() => _parentChangeset.Changes.OfType<ResolvedJoin>().ShouldBeEmpty();
    [Fact] void should_keep_only_the_child_added() => _parentChangeset.Changes.ShouldContainOnly(_childAdded);
}
