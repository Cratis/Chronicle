// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

/// <summary>
/// Characterizes the current behavior: consolidating a <see cref="ResolvedJoin"/> into a matching
/// <see cref="ChildAdded"/> removes it from the parent, so any change added to the join's changeset
/// afterwards is silently dropped. The second assertion pins that data loss and is expected to change.
/// </summary>
public class and_resolved_join_receives_changes_after_being_consolidated : given.a_changeset_with_parent
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
        childDict["description"] = "Original Description";

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
        _joinChangeset.Add(new PropertiesChanged<ExpandoObject>(
            _childInitialState,
            [new PropertyDifference(new PropertyPath("name"), "Original Name", "Updated Name", ArrayIndexers.NoIndexers)]));
    }

    void Because() => _joinChangeset.Add(new PropertiesChanged<ExpandoObject>(
        _childInitialState,
        [new PropertyDifference(new PropertyPath("description"), "Original Description", "Updated Description", ArrayIndexers.NoIndexers)]));

    [Fact] void should_apply_the_first_property_change_to_the_child() => ((IDictionary<string, object?>)_child)["name"].ShouldEqual("Updated Name");
    [Fact] void should_lose_the_later_property_change() => ((IDictionary<string, object?>)_child)["description"].ShouldEqual("Original Description");
    [Fact] void should_remove_resolved_join() => _parentChangeset.Changes.OfType<ResolvedJoin>().ShouldBeEmpty();
    [Fact] void should_keep_only_the_child_added() => _parentChangeset.Changes.ShouldContainOnly(_childAdded);
}
