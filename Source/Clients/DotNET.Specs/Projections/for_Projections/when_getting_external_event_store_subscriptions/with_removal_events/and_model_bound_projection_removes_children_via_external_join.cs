// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions.with_removal_events;

public class and_model_bound_projection_removes_children_via_external_join : given.projections_with_removal_events
{
    void Establish() => _clientArtifacts.ModelBoundProjections.Returns([typeof(TheReadModel)]);

    async Task Because()
    {
        await _projections.Discover();
        _result = _projections.GetExternalEventStoreSubscriptions();
    }

    [Fact] void should_subscribe_to_the_external_store() => _result.Single().EventStoreName.ShouldEqual(ExternalStoreName);
    [Fact] void should_include_the_removal_event_alongside_the_creating_event() => _result.Single().EventTypeIds.ShouldContainOnly(
        new EventTypeId(nameof(ExternalCreated)), new EventTypeId(nameof(ExternalRemovedViaJoin)));

    [FromEvent<ExternalCreated>]
    public record TheReadModel(
        string Id,
        [ChildrenFrom<ExternalCreated>]
        [RemovedWithJoin<ExternalRemovedViaJoin>]
        IEnumerable<Item> Items);

    public record Item(string Id);
}
