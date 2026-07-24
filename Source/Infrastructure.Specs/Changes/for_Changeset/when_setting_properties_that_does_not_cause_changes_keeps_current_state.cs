// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_setting_properties_that_does_not_cause_changes_keeps_current_state : Specification
{
    Changeset<ExpandoObject, ExpandoObject> _changeset;
    ExpandoObject _initialState;
    ExpandoObject _source;
    IEnumerable<PropertyMapper<ExpandoObject, ExpandoObject>> _propertyMappers;
    IObjectComparer _objectsComparer;
    object _currentStateBefore;

    void Establish()
    {
        _initialState = new();
        ((dynamic)_initialState).Integer = 42;
        ((dynamic)_initialState).String = "Forty Two";

        _propertyMappers =
        [
            (_, target, __) =>
            {
                ((dynamic)target).Integer = 42;
                return new PropertyDifference("integer", 42, 42);
            },
            (_, target, __) =>
            {
                ((dynamic)target).String = "Forty Two";
                return new PropertyDifference("string", "Forty Two", "Forty Two");
            }
        ];

        _source = new ExpandoObject();
        _objectsComparer = Substitute.For<IObjectComparer>();
        _changeset = new(_objectsComparer, _source, _initialState);
        _currentStateBefore = _changeset.CurrentState;
    }

    void Because() => _changeset.SetProperties(_propertyMappers, ArrayIndexers.NoIndexers);

    [Fact] void should_not_record_any_change() => _changeset.Changes.ShouldBeEmpty();
    [Fact] void should_not_adopt_a_new_cloned_state() => ReferenceEquals(_changeset.CurrentState, _currentStateBefore).ShouldBeTrue();
    [Fact] void should_keep_the_initial_state_as_current_state() => _changeset.CurrentState.ShouldEqual(_initialState);
}
