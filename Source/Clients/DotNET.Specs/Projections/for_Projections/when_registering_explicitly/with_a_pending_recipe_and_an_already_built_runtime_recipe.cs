// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class with_a_pending_recipe_and_an_already_built_runtime_recipe : given.projections_with_competing_ids
{
    int _runtimeBuilds;
    IProjectionHandler _existing;

    async Task Establish()
    {
        _projections.Add(new DeclarativeExplicitProjection<FirstModel>(projection => projection.From<CustomerRegistered>(), SharedId));
        _existing = await _projections.Register<SecondModel>(
            projection =>
            {
                _runtimeBuilds++;
                projection.From<CustomerRegistered>();
            },
            "runtime-id");
    }

    async Task Because() => await _projections.Register<Inventory>(projection => projection.From<ItemAdded>(), "inventory-id");

    [Fact] void should_not_rebuild_the_existing_runtime_recipe() => _runtimeBuilds.ShouldEqual(1);
    [Fact] void should_keep_the_existing_runtime_handler() => _projections.GetAllHandlers().ShouldContain(_existing);
    [Fact] void should_not_publish_the_pending_owner_during_preflight() => _projections.HasFor<FirstModel>().ShouldBeFalse();
    [Fact] void should_publish_both_runtime_definitions() => _projections.Definitions.Count.ShouldEqual(2);
}
