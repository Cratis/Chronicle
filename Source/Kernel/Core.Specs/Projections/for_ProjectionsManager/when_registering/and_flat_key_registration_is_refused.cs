// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

[PerFactSpecificationLifecycle]
public class and_flat_key_registration_is_refused : given.a_projections_manager_grain
{
    const string FlatKey = "$flatComposite(customerId=userId,orderNumber=name)";

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    public async Task should_refuse_switching_key_mode_before_registering_or_persisting(bool wasFlat, bool becomesFlat, bool rename)
    {
        _eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Sinks.SupportsFlatCompositeKeys(Arg.Any<ReadModelDefinition>()).Returns(true);
        var stored = Definition(wasFlat);
        _state.Projections = [stored];
        _readModelDefinitions = [CreateReadModelDefinition("read-model")];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Different);

        var incoming = Definition(becomesFlat);
        if (rename) incoming = incoming with { Identifier = "renamed" };
        var exception = await Catch.Exception(() => _grain.Register([incoming]));

        exception.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
        exception.Message.ShouldContain("key mode");
        await ShouldNotHaveRegistered();
        _state.Projections.ShouldContainOnly(stored);
    }

    [Fact]
    public async Task should_refuse_an_unsupported_sink_before_registering_or_persisting()
    {
        _readModelDefinitions = [CreateReadModelDefinition("read-model")];
        var exception = await Catch.Exception(() => _grain.Register([Definition(true)]));

        exception.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
        await ShouldNotHaveRegistered();
        _state.Projections.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_accept_a_new_flat_projection_on_a_supported_sink()
    {
        _eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Sinks.SupportsFlatCompositeKeys(Arg.Any<ReadModelDefinition>()).Returns(true);
        _readModelDefinitions = [ReadModel()];
        var definition = Definition(true);
        await _grain.Register([definition]);
        _state.Projections.ShouldContainOnly(definition);
        await _projectionGrain.Received().SetDefinition(definition);
    }

    [Fact]
    public async Task should_leave_an_identical_flat_registration_unchanged()
    {
        _eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Sinks.SupportsFlatCompositeKeys(Arg.Any<ReadModelDefinition>()).Returns(true);
        _readModelDefinitions = [ReadModel()];
        var definition = Definition(true);
        _state.Projections = [definition];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
        await _grain.Register([definition]);
        await ShouldNotHaveRegistered();
        _state.Projections.ShouldContainOnly(definition);
    }

    static ReadModelDefinition ReadModel() => CreateReadModelDefinition("read-model") with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"customerId":{"type":"string"},"orderNumber":{"type":"string"}}}""")
        }
    };

    async Task ShouldNotHaveRegistered()
    {
        await _projectionsServiceClient.DidNotReceive().Register((EventStoreName)EventStore, Arg.Any<IEnumerable<ProjectionDefinition>>());
        await _projectionGrain.DidNotReceive().SetDefinition(Arg.Any<ProjectionDefinition>());
        await _stateStorage.DidNotReceive().WriteStateAsync();
    }

    static ProjectionDefinition Definition(bool flat) => CreateDefinition("projection", "read-model") with
    {
        SubscribesToAllEvents = false,
        From = new Dictionary<EventType, FromDefinition>
        {
            [new EventType("event", 1)] = new(new Dictionary<Properties.PropertyPath, string>(), flat ? FlatKey : "$eventSourceId", null)
        }
    };
}
