// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_converting_an_object;

public class and_there_is_an_extra_member : given.an_expando_payload
{
    object _stringResult;

    void Establish()
    {
        _members["value"] = "42";
        _members["unit"] = "items";
    }

    void Because()
    {
        _objectResult = Resolve(JsonObjectType.Object);
        _unspecifiedResult = Resolve(JsonObjectType.None);
        _stringResult = Resolve(JsonObjectType.String);
    }

    [Fact] void should_preserve_the_object_destination() => ReferenceEquals(_objectResult, _input).ShouldBeTrue();
    [Fact] void should_preserve_the_unspecified_destination() => ReferenceEquals(_unspecifiedResult, _input).ShouldBeTrue();
    [Fact] void should_not_unwrap_for_a_string_destination() => ReferenceEquals(_stringResult, _input).ShouldBeTrue();
    [Fact] void should_preserve_the_value_member() => ((IDictionary<string, object?>)_objectResult)["value"].ShouldEqual("42");
    [Fact] void should_preserve_the_extra_member() => ((IDictionary<string, object?>)_objectResult)["unit"].ShouldEqual("items");
}
