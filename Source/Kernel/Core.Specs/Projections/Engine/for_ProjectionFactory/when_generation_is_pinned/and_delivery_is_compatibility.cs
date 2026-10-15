// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_delivery_is_compatibility : given.a_projection_with_generations
{
    void Establish()
    {
        var secondFrom = _definition.From[_second];
        _definition.From.Remove(_second);
        _second = new(_first.Id, 2);
        _definition.From[_second] = secondFrom;
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Compatibility };
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        Project(_projection, Event(_second, "migrated"));
    }

    [Fact] void should_keep_identifier_only_dispatch_for_the_first_clause() => ((IDictionary<string, object?>)_state)["oldName"].ShouldEqual("migrated");
    [Fact] void should_keep_identifier_only_dispatch_for_the_second_clause() => ((IDictionary<string, object?>)_state)["newName"].ShouldEqual("migrated");
}
