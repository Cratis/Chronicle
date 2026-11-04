// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_converting_an_object.and_it_has_only_a_value_member;

public class with_an_object : given.an_expando_payload
{
    ExpandoObject _nested;

    void Establish()
    {
        _nested = new();
        ((IDictionary<string, object?>)_nested)["message"] = "kept";
        _members["value"] = _nested;
    }

    void Because()
    {
        _objectResult = Resolve(JsonObjectType.Object);
        _unspecifiedResult = Resolve(JsonObjectType.None);
    }

    [Fact] void should_preserve_the_object_destination() => ReferenceEquals(_objectResult, _input).ShouldBeTrue();
    [Fact] void should_preserve_the_unspecified_destination() => ReferenceEquals(_unspecifiedResult, _input).ShouldBeTrue();
    [Fact] void should_preserve_the_nested_object_for_the_object_destination() => ReferenceEquals(((IDictionary<string, object?>)_objectResult)["value"], _nested).ShouldBeTrue();
    [Fact] void should_preserve_the_nested_object_for_the_unspecified_destination() => ReferenceEquals(((IDictionary<string, object?>)_unspecifiedResult)["value"], _nested).ShouldBeTrue();
}
