// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_clearing_nested;

public class and_current_state_has_the_nested_property : when_adding_changes.given.a_changeset
{
    IDictionary<string, object?> _originalOuter;
    IDictionary<string, object?> _currentOuter;

    void Establish()
    {
        dynamic outer = new ExpandoObject();
        outer.info = new ExpandoObject();
        outer.info.name = "Before";
        ((IDictionary<string, object?>)_initialState)["outer"] = outer;
    }

    void Because()
    {
        _changeset.ClearNested("outer.info", ArrayIndexers.NoIndexers);
        _originalOuter = (IDictionary<string, object?>)((IDictionary<string, object?>)_changeset.InitialState)["outer"]!;
        _currentOuter = (IDictionary<string, object?>)((IDictionary<string, object?>)_changeset.CurrentState)["outer"]!;
    }

    [Fact] void should_clear_the_current_state() => _currentOuter["info"].ShouldBeNull();
    [Fact] void should_leave_the_initial_state_unchanged() => _originalOuter["info"].ShouldNotBeNull();
}
