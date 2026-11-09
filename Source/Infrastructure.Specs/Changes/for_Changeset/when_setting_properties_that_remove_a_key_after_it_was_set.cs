// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_setting_properties_that_remove_a_key_after_it_was_set : Specification
{
    static readonly PropertyPath _key = new("slots.first");

    Changeset<object, ExpandoObject> _changeset;
    PropertyDifference[] _differences;

    void Establish()
    {
        var initialState = new ExpandoObject();
        var slots = new ExpandoObject();
        ((IDictionary<string, object?>)slots)["second"] = "series-2";
        ((IDictionary<string, object?>)initialState)["slots"] = slots;
        _changeset = new Changeset<object, ExpandoObject>(Substitute.For<IObjectComparer>(), new object(), initialState);
    }

    void Because()
    {
        _changeset.SetProperties(
            [(_, target, indexers) =>
            {
                _key.SetValue(target, "series-1", indexers);
                return new PropertyDifference(_key, null, "series-1", indexers);
            }],
            ArrayIndexers.NoIndexers);
        _changeset.SetProperties(
            [(_, target, indexers) =>
            {
                _key.RemoveValue(target, indexers);
                return PropertyDifference.Removal(_key, "series-1", indexers);
            }],
            ArrayIndexers.NoIndexers);
        _differences = [.. _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(_ => _.Differences)];
    }

    IDictionary<string, object?> Slots => (IDictionary<string, object?>)((IDictionary<string, object?>)_changeset.CurrentState)["slots"]!;

    [Fact] void should_not_hold_the_key_in_the_current_state() => Slots.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_keep_the_other_key() => Slots["second"].ShouldEqual("series-2");
    [Fact] void should_consolidate_into_one_difference() => _differences.Length.ShouldEqual(1);
    [Fact] void should_consolidate_into_a_removal() => _differences[0].IsRemoval.ShouldBeTrue();
}
