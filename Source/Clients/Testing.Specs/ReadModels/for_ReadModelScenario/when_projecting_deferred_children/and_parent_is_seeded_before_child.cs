// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_deferred_children;

public class and_parent_is_seeded_before_child : Specification
{
    ReadModelScenario<DeferredBoard> _scenario;
    EventSourceId _id;
    Guid _groupId;
    Guid _itemId;

    void Establish()
    {
        _scenario = new();
        _groupId = Guid.NewGuid();
        _id = new EventSourceId(_groupId);
        _itemId = Guid.NewGuid();
    }

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(
        new DeferredGroupAdded(_groupId, "Group"),
        new DeferredItemAdded(_groupId, _itemId, "Item", 7));

    [Fact] void should_materialize_the_child() => _scenario.Instance!.Groups.Single().Items.Single().Id.ShouldEqual(_itemId);
    [Fact] void should_apply_the_root_mapping_once() => _scenario.Instance!.TotalAmount.ShouldEqual(7);
}
