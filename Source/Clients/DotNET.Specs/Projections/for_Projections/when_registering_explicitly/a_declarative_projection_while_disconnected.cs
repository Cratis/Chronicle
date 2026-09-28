// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_declarative_projection_while_disconnected : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;
    ProjectionDefinition _definition;

    async Task Because()
    {
        _handler = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
        _definition = _projections.Definitions.Single();
    }

    [Fact] void should_know_the_read_model() => _projections.HasFor<Inventory>().ShouldBeTrue();
    [Fact] void should_identify_the_projection_by_the_read_model_name() => _handler.Id.Value.ShouldEqual(typeof(Inventory).FullName);
    [Fact] void should_resolve_the_projection_by_read_model() => _projections.GetProjectionIdForModel<Inventory>().ShouldEqual(_handler.Id);
    [Fact] void should_hand_out_the_handler_for_the_read_model() => _handler.ReadModelType.ShouldEqual(typeof(Inventory));
    [Fact] void should_include_the_handler_among_all_handlers() => _projections.GetAllHandlers().ShouldContain(_handler);
    [Fact] void should_build_the_definition_from_the_callback() => _definition.From.Keys.Single().Id.ShouldEqual(nameof(ItemAdded));
    [Fact] void should_be_active() => _definition.IsActive.ShouldBeTrue();
    [Fact] void should_report_the_read_model_as_registered() => _projections.ArtifactRegistrations.Single().ArtifactType.ShouldEqual(typeof(Inventory));
    [Fact] void should_not_send_anything_to_the_kernel() => _registrations.ShouldBeEmpty();
    [Fact] void should_not_register_the_read_model_with_the_kernel() => _readModels.DidNotReceive().Register<Inventory>();
}
