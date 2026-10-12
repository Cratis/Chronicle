// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_pinned_pdl_definition_is_not_rewindable : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        _readModelDefinitions = [CreateReadModelDefinition("model") with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema } }];
        var language = new LanguageService(new Generator(), new KnownInstancesOf<IProjectionCodeGenerator>());
        EventTypeSchema[] schemas = [new(new EventType("PersonRegistered", 1), EventTypeOwner.Client, EventTypeSource.Code, schema)];
        _definition = language.Compile("projection Person => model\n  from PersonRegistered", ProjectionOwner.Client, _readModelDefinitions, schemas, EventGenerationDelivery.Pinned).Match(value => value, _ => throw new Exception("The PDL definition could not be compiled."));
    }

    async Task Because() => await _grain.Register([_definition]);

    [Fact] void should_keep_the_pdl_rewindability_setting() => _definition.IsRewindable.ShouldBeFalse();
    [Fact] void should_subscribe_as_replayable() => _observerGrain.Received(1).SubscribeWithGenerationDelivery<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, isReplayable: true, reactivateRetired: true);
}
