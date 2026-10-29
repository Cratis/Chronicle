// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_stable_model_bound_identity_without_discovery : given.projections_with_competing_ids
{
    IProjectionHandler _handler;

    async Task Because()
    {
        _handler = await _projections.Register<FirstModel>();
        await _projections.Register<FirstModel>();
    }

    [Fact] void should_use_the_stable_handler_id() => _handler.Id.ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_use_the_stable_definition_id() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_keep_one_handler_on_repeated_registration() => _projections.GetAllHandlers().Count().ShouldEqual(1);
    [Fact] void should_resolve_the_id_by_read_model() => _projections.GetProjectionIdForModel<FirstModel>().ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_not_contact_the_server_while_disconnected() => _registrations.ShouldBeEmpty();
}
