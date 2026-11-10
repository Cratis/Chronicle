// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_event_content_dictionary_key_targets_a_non_dynamic_property : given.a_projection_factory_and_an_every_event_mapping
{
    const string Schema = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "count": { "type": "integer" }
          }
        }
        """;

    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => Create("count.$eventContent.slot", WellKnownExpressions.Increment, Schema));

    [Fact] void should_throw_dynamic_property_path_on_non_dynamic_property() =>
        _result.ShouldBeOfExactType<DynamicPropertyPathOnNonDynamicProperty>();
}
