// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class the_same_owner_as_a_pending_recipe : given.projections_with_competing_ids
{
    IProjectionHandler _handler;
    int _definitionsBeforeDiscovery;

    void Establish() => _projections.Add(new DeclarativeExplicitProjection<FirstModel>(_ => { }, SharedId));

    async Task Because()
    {
        _handler = await _projections.Register<FirstModel>(projection => projection.From<CustomerRegistered>(), SharedId);
        _definitionsBeforeDiscovery = _projections.Definitions.Count;
        await _projections.Discover();
    }

    [Fact] void should_allow_the_same_owner_overlay() => _handler.Id.ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_publish_only_the_runtime_definition_before_discovery() => _definitionsBeforeDiscovery.ShouldEqual(1);
    [Fact] void should_keep_one_definition_after_discovery() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_keep_the_runtime_recipe_after_discovery() => _projections.Definitions.Single().From.Count.ShouldEqual(1);
    [Fact] void should_keep_one_handler_after_discovery() => _projections.GetAllHandlers().Count().ShouldEqual(1);
    [Fact] void should_not_register_remotely_while_disconnected() => _registrations.ShouldBeEmpty();
}
