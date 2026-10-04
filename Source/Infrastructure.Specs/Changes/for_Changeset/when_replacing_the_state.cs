// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_replacing_the_state : Specification
{
    Changeset<ExpandoObject, ExpandoObject> _changeset;
    ExpandoObject _initialState;
    ExpandoObject _state;
    PropertyDifference[] _differences;

    void Establish()
    {
        _initialState = new();
        _state = new();
        ((dynamic)_state).Total = 60;
        _differences = [new PropertyDifference("total", null, 60)];
        _changeset = new(Substitute.For<IObjectComparer>(), new ExpandoObject(), _initialState);
    }

    void Because() => _changeset.ReplaceState(_state, _differences);

    [Fact] void should_make_the_state_current() => _changeset.CurrentState.ShouldEqual(_state);
    [Fact] void should_keep_the_initial_state() => _changeset.InitialState.ShouldEqual(_initialState);
    [Fact] void should_record_the_differences() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.Single()).Differences.ShouldEqual(_differences);
    [Fact] void should_record_the_state_on_the_change() => _changeset.Changes.Single().State.ShouldEqual(_state);
}
