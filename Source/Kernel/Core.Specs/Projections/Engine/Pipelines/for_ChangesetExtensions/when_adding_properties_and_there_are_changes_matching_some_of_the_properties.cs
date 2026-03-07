// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public class when_adding_properties_and_there_are_changes_matching_some_of_the_properties : Specification
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    ExpandoObject _source;
    ExpandoObject _initialModelState;

    PropertiesChanged<ExpandoObject> _firstPropertiesChanged;
    PropertiesChanged<ExpandoObject> _secondPropertiesChanged;
    ChildAdded _firstChildAdded;
    ChildAdded _secondChildAdded;
    List<PropertiesChanged<ExpandoObject>> _changes;

    void Establish()
    {
        _initialModelState = new ExpandoObject();
        _source = new ExpandoObject();
        dynamic sourceAsDynamic = _source;
        sourceAsDynamic.FirstProperty = "First value";
        sourceAsDynamic.SecondProperty = "Second value";

#pragma warning disable IDE0301 // Use collection initializers
        sourceAsDynamic.ThirdProperty = Array.Empty<ExpandoObject>();
        sourceAsDynamic.ForthProperty = Array.Empty<ExpandoObject>();
#pragma warning restore IDE0301 // Use collection initializers

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changes = [];
        _changeset
            .When(_ => _.Add(Arg.Any<Change>()))
            .Do(_ => _changes.Add(_.Arg<PropertiesChanged<ExpandoObject>>()));

        _firstPropertiesChanged = new PropertiesChanged<ExpandoObject>(_initialModelState,
        [
            new PropertyDifference("SecondProperty", "Second original value", "Second new value"),
        ]);

        _secondPropertiesChanged = new PropertiesChanged<ExpandoObject>(_initialModelState,
        [
            new PropertyDifference("FifthProperty", "Fifth original value", "Fifth new value")
        ]);

        _firstChildAdded = new ChildAdded(new ExpandoObject(), "[ForthProperty]", "", 0, ArrayIndexers.NoIndexers);
        _secondChildAdded = new ChildAdded(new ExpandoObject(), "[SixthProperty]", "", 0, ArrayIndexers.NoIndexers);

        _changeset.Changes.Returns(
        [
            _firstPropertiesChanged,
            _secondPropertiesChanged,
            _firstChildAdded,
            _secondChildAdded
        ]);
    }

    void Because() => _changeset.AddPropertiesFrom(_source);

    [Fact] void should_contain_only_one_change() => _changes.Count.ShouldEqual(1);
    [Fact] void should_contain_difference_for_first_property() => _changes.ShouldContain(_ => _.Differences.Any(_ => _.PropertyPath == "FirstProperty"));
    [Fact] void should_hold_correct_value_for_first_property_difference() => _changes[0].Differences.First(_ => _.PropertyPath == "FirstProperty").Changed.ShouldEqual("First value");
    [Fact] void should_contain_difference_for_third_property() => _changes.ShouldContain(_ => _.Differences.Any(_ => _.PropertyPath == "ThirdProperty"));
    [Fact] void should_hold_correct_value_for_third_property_difference() => _changes[0].Differences.First(_ => _.PropertyPath == "ThirdProperty").Changed.ShouldEqual(Array.Empty<ExpandoObject>());
}
