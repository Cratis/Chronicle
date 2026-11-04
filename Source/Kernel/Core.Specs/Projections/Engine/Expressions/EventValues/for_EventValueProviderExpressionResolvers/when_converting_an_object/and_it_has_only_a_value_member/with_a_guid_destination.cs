// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_converting_an_object.and_it_has_only_a_value_member;

public class with_a_guid_destination : given.an_expando_payload
{
    static readonly Guid _identifier = new("0b7325dd-7a25-4681-9ab7-c387a6073547");
    object _result;

    void Establish() => _members["value"] = _identifier.ToString();

    void Because() => _result = Resolve(JsonObjectType.String, "guid");

    [Fact] void should_unwrap_and_convert_the_value() => _result.ShouldEqual(_identifier);
    [Fact] void should_return_a_guid() => _result.ShouldBeOfExactType<Guid>();
}
