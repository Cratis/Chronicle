// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangeCollectionPathExtensions;

public class when_applying_a_removal_to_state : Specification
{
    ExpandoObject _state;
    ExpandoObject _result;

    void Establish()
    {
        _state = new ExpandoObject();
        var slots = new ExpandoObject();
        ((IDictionary<string, object?>)slots)["first"] = "series-1";
        ((IDictionary<string, object?>)slots)["second"] = "series-2";
        ((IDictionary<string, object?>)_state)["slots"] = slots;
    }

    void Because()
    {
        var change = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [PropertyDifference.Removal(new PropertyPath("slots.first"), "series-1")]);
        _result = change.ApplyToStateWithoutChildOperationConflicts(_state, new HashSet<PropertyPath>());
    }

    IDictionary<string, object?> Slots => (IDictionary<string, object?>)((IDictionary<string, object?>)_result)["slots"]!;

    [Fact] void should_remove_the_key_rather_than_set_it_to_null() => Slots.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_keep_the_other_key() => Slots["second"].ShouldEqual("series-2");
}
