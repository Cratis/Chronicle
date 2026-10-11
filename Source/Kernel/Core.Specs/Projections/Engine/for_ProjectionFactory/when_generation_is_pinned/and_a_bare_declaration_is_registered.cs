// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_bare_declaration_is_registered : given.a_projection_with_generations
{
    ProjectionDefinition _compiled;
    ProjectionDefinition _compatibility;

    void Establish() => _schemas = [.. _schemas, new(new EventType("PersonRegistered", 2), EventTypeOwner.Client, EventTypeSource.Code, _schemas[1].Schema)];

    async Task Because()
    {
        var language = new LanguageService(new Generator(), new KnownInstancesOf<IProjectionCodeGenerator>());
        _compiled = language.Compile("projection Person => model\n  from PersonRegistered", ProjectionOwner.Client, [_readModel], _schemas, EventGenerationDelivery.Pinned).Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _compatibility = language.Compile("projection Person => model\n  from PersonRegistered", ProjectionOwner.Client, [_readModel], _schemas).Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        EventTypeSchema[] withLaterGeneration = [.. _schemas, new(new EventType("PersonRegistered", 3), EventTypeOwner.Client, EventTypeSource.Code, _schemas[1].Schema)];
        _projection = await _factory.Create("store", "namespace", _compiled, _readModel, withLaterGeneration);
    }

    [Fact] void should_pin_the_latest_registered_generation() => _compiled.From.Keys.Single().Generation.Value.ShouldEqual(2U);
    [Fact] void should_record_pinned_delivery() => _compiled.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_keep_the_persisted_pin_after_a_new_generation_is_registered() => _projection.Accepts(new EventType("PersonRegistered", 2)).ShouldBeTrue();
    [Fact] void should_not_move_the_stored_pin_to_the_new_generation() => _projection.Accepts(new EventType("PersonRegistered", 3)).ShouldBeFalse();
    [Fact] void should_preserve_compatibility_bare_name_behavior() => _compatibility.From.Keys.Single().Generation.Value.ShouldEqual(1U);
}
