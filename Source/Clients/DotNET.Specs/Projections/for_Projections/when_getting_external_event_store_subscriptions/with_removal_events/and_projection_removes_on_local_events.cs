// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions.with_removal_events;

public class and_projection_removes_on_local_events : given.projections_with_removal_events
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

    [Fact] void should_not_subscribe_to_an_external_store() => _result.ShouldBeEmpty();

    public record TheReadModel(string Id, IEnumerable<Item> Items);
    public record Item(string Id);

    public class TheProjection : IProjectionFor<TheReadModel>
    {
        public void Define(IProjectionBuilderFor<TheReadModel> builder) => builder
            .From<LocalCreated>()
            .RemovedWith<LocalRemoved>()
            .Children(model => model.Items, children => children.RemovedWithJoin<LocalRemoved>());
    }
}
