// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions.with_removal_events;

public class and_projection_removes_on_event_from_external_store : given.projections_with_removal_events
{
    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(TheProjection)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheReadModel>>(typeof(TheProjection))
            .Returns(new TheProjection());
    }

    async Task Because()
    {
        await _projections.Discover();
        _result = _projections.GetExternalEventStoreSubscriptions();
    }

    [Fact] void should_subscribe_to_the_external_store() => _result.Single().EventStoreName.ShouldEqual(ExternalStoreName);
    [Fact] void should_include_the_removal_event_once_alongside_from_and_join() => _result.Single().EventTypeIds.ShouldContainOnly(
        new EventTypeId(nameof(ExternalCreated)), new EventTypeId(nameof(ExternalJoined)), new EventTypeId(nameof(ExternalRemoved)));

    public record TheReadModel(string Id, IEnumerable<Item> Items);
    public record Item(string Id);

    public class TheProjection : IProjectionFor<TheReadModel>
    {
        public void Define(IProjectionBuilderFor<TheReadModel> builder) => builder
            .From<ExternalCreated>()
            .Join<ExternalJoined>(join => join.On(model => model.Id))
            .RemovedWith<ExternalRemoved>()
            .Children(model => model.Items, children => children.RemovedWith<ExternalRemoved>());
    }
}
