// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_child_added_with_initial_state_containing_nested_arrays : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _idProperty;
    ExpandoObject _childInitialState;
    ArrayIndexers _firstArrayIndexers;
    ArrayIndexers _secondArrayIndexers;
    IDictionary<string, object?> _firstChildDict;
    IDictionary<string, object?> _secondChildDict;

    void Establish()
    {
        _itemsProperty = new PropertyPath("regions");
        _idProperty = PropertyPath.CreateFrom([new PropertyName("regionId")]);

        _childInitialState = new ExpandoObject();
        var childStateDict = (IDictionary<string, object?>)_childInitialState;
        childStateDict["events"] = new List<object>();
        childStateDict["stickyNotes"] = new List<object>();

        _firstArrayIndexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, "region-1")]);
        _secondArrayIndexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, "region-2")]);
    }

    void Because()
    {
        _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, "region-1", [], _firstArrayIndexers, _childInitialState);
        _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, "region-2", [], _secondArrayIndexers, _childInitialState);

        var children = _changeset.Changes.OfType<ChildAdded>().ToArray();
        _firstChildDict = (IDictionary<string, object?>)children[0].Child;
        _secondChildDict = (IDictionary<string, object?>)children[1].Child;
    }

    [Fact] void should_add_two_child_added_changes() => _changeset.Changes.OfType<ChildAdded>().Count().ShouldEqual(2);

    [Fact] void should_initialize_events_array_on_first_child() => _firstChildDict.ContainsKey("events").ShouldBeTrue();

    [Fact] void should_initialize_sticky_notes_array_on_first_child() => _firstChildDict.ContainsKey("stickyNotes").ShouldBeTrue();

    [Fact] void should_initialize_events_array_on_second_child() => _secondChildDict.ContainsKey("events").ShouldBeTrue();

    [Fact] void should_initialize_sticky_notes_array_on_second_child() => _secondChildDict.ContainsKey("stickyNotes").ShouldBeTrue();

    [Fact] void should_set_identifier_on_first_child() => _firstChildDict["regionId"].ShouldEqual("region-1");

    [Fact] void should_set_identifier_on_second_child() => _secondChildDict["regionId"].ShouldEqual("region-2");

    [Fact]
    void should_not_share_events_list_between_children() =>
        ReferenceEquals(_firstChildDict["events"], _secondChildDict["events"]).ShouldBeFalse();
}
