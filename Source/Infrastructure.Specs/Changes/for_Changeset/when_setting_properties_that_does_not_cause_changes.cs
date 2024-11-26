// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_setting_properties_that_does_not_cause_changes : Specification
{
    Changeset<ExpandoObject, ExpandoObject> _changeset;
    ExpandoObject _initialState;
    ExpandoObject _source;
    IEnumerable<PropertyMapper<ExpandoObject, ExpandoObject>> _propertyMappers;
    IObjectComparer _objectsComparer;

    void Establish()
    {
        _initialState = new();

        ((dynamic)_initialState).Integer = 42;
        ((dynamic)_initialState).String = "Forty Two";
        dynamic nested = ((dynamic)_initialState).Nested = new ExpandoObject();
        nested.Integer = 43;
        nested.String = "Forty Three";

        _propertyMappers =
        [
                (_, target, __) =>
                    {
                        ((dynamic)target).Integer = 44;
                        return new PropertyDifference("integer", 44, 44);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).String = "Forty Four";
                        return new PropertyDifference("string", "Forty Four", "Forty Four");
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.Integer = 45;
                        return new PropertyDifference("nested.integer", 45, 45);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.String = "Forty Five";
                        return new PropertyDifference("nested.string", "Forty Five", "Forty Five");
                    }
        ];

        _source = new ExpandoObject();

        _objectsComparer = Substitute.For<IObjectComparer>();
        _changeset = new(_objectsComparer, _source, _initialState);
    }

    void Because() => _changeset.SetProperties(_propertyMappers, ArrayIndexers.NoIndexers);

    [Fact] void should_not_have_any_changes() => _changeset.Changes.Count().ShouldEqual(0);
}
