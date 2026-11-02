// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_converting_an_object.and_it_has_only_a_value_member;

public class with_a_string_destination : given.an_expando_payload
{
    object _result;

    void Establish() => _members["value"] = "kept";

    void Because() => _result = Resolve(JsonObjectType.String);

    [Fact] void should_unwrap_the_value() => _result.ShouldEqual("kept");
    [Fact] void should_return_a_string() => _result.ShouldBeOfExactType<string>();
}
