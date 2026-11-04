// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Projections;
using ProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_saving;

public class and_the_inferred_read_model_does_not_cover_the_projection_targets : given.an_inferred_order_overview
{
    SaveProjectionResult _result;

    async Task Because() => _result = await _service.Save(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = Declaration,
        DraftReadModel = new() { Identifier = "orderOverview", ContainerName = "orderOverview", DisplayName = "OrderOverview" }
    });

    [Fact] void should_return_one_error() => _result.Errors.Count().ShouldEqual(1);
    [Fact] void should_locate_the_error_at_the_declaration_start() => (_result.Errors.Single().Line, _result.Errors.Single().Column).ShouldEqual((1, 1));
    [Fact] void should_explain_how_to_use_an_explicit_read_model() => _result.Errors.Single().Message.ShouldContain("=> <ReadModel>");
    [Fact] void should_name_the_every_target() => _result.Errors.Single().Message.ShouldContain("LastUpdated");
    [Fact] void should_name_the_literal_target() => _result.Errors.Single().Message.ShouldContain("IsShipped");
    [Fact] void should_name_the_counter() => _result.Errors.Single().Message.ShouldContain("ItemCount");
    [Fact] void should_name_the_child_collection() => _result.Errors.Single().Message.ShouldContain("Lines");
    [Fact] void should_name_the_join_target() => _result.Errors.Single().Message.ShouldContain("ProductName");
    [Fact] void should_not_register_the_read_model() => _readModelsManager.DidNotReceive().RegisterSingle(Arg.Any<ReadModelDefinition>());
    [Fact] void should_not_register_the_projection() => _projectionsManager.DidNotReceive().Register(Arg.Any<IEnumerable<ProjectionDefinition>>());
}
