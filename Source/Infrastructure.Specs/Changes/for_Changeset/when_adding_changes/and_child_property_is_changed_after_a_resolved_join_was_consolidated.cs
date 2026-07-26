// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

/// <summary>
/// A change consolidated from a <see cref="ResolvedJoin"/> is applied exactly once, so a value set on
/// the child after the join was consolidated survives every later consolidation pass.
/// </summary>
public class and_child_property_is_changed_after_a_resolved_join_was_consolidated : given.a_changeset_with_parent
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
            [new PropertyDifference(new PropertyPath("name"), "Original Name", "Joined Name", ArrayIndexers.NoIndexers)]));

        _parentChangeset.Add(new PropertiesChanged<ExpandoObject>(
            _parentInitialState,
            [new PropertyDifference(new PropertyPath("name"), "Joined Name", "Final Name", _arrayIndexers)]));
    }

    void Because() => _joinChangeset.Add(new PropertiesChanged<ExpandoObject>(
        _childInitialState,
        [new PropertyDifference(new PropertyPath("description"), "Original Description", "Joined Description", ArrayIndexers.NoIndexers)]));

    [Fact] void should_not_reapply_the_already_consolidated_property() => ((IDictionary<string, object?>)_child)["name"].ShouldEqual("Final Name");
    [Fact] void should_apply_the_new_property_change_to_the_child() => ((IDictionary<string, object?>)_child)["description"].ShouldEqual("Joined Description");
    [Fact] void should_keep_only_the_child_added() => _parentChangeset.Changes.ShouldContainOnly(_childAdded);
}
