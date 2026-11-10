// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_removing_a_property_without_a_dynamic_key : given.a_projection_factory_and_an_every_event_mapping
{
    const string Schema = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "slots": { "type": "object", "additionalProperties": { "type": "string" } }
          }
        }
        """;

    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => Create("slots", WellKnownExpressions.Remove, Schema));

    [Fact] void should_fail_because_removing_needs_a_dynamic_key() =>
        _result.ShouldBeOfExactType<RemoveRequiresDynamicPropertyPath>();
}
