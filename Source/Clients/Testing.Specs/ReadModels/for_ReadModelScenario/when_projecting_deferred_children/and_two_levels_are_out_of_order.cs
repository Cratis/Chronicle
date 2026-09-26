// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_deferred_children;

public class and_two_levels_are_out_of_order : Specification
{
    ReadModelScenario<DeferredBoard> _scenario;
    EventSourceId _id;
    Guid _groupId;
    Guid _sectionId;
    Guid _itemId;

    void Establish()
    {
        _scenario = new();
        _groupId = Guid.NewGuid();
        _id = new EventSourceId(_groupId);
        _sectionId = Guid.NewGuid();
        _itemId = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new DeferredBoardOpened());
        await _scenario.Given.ForEventSource(new EventSourceId(_itemId)).Events(new DeferredSectionItemAdded(_groupId, _sectionId, _itemId, "Item", 7));
        await _scenario.Given.ForEventSource(new EventSourceId(_sectionId)).Events(new DeferredSectionAdded(_groupId, _sectionId, "Section", 1));
        await _scenario.Given.ForEventSource(_id).Events(new DeferredGroupAdded(_groupId, "Group"));
    }

    [Fact] void should_materialize_the_intermediate_child() => _scenario.Instance!.Groups.Single().Sections.Single().Id.ShouldEqual(_sectionId);
    [Fact] void should_materialize_the_leaf_after_the_intermediate_child() => _scenario.Instance!.Groups.Single().Sections.Single().Items.Single().Id.ShouldEqual(_itemId);
    [Fact] void should_not_reapply_either_root_mapping() => (_scenario.Instance!.NestedItemAmount, _scenario.Instance.SectionAmount).ShouldEqual((7, 1));
}
