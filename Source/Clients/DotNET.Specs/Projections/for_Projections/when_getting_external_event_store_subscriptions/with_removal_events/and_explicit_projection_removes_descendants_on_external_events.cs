// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions.with_removal_events;

public class and_explicit_projection_removes_descendants_on_external_events : given.projections_with_removal_events
{
    async Task Because()
    {
        await _projections.Register<TheReadModel>(builder => builder
            .From<ExternalCreated>()
            .Children(model => model.Items, children => children
                .RemovedWith<ExternalRemoved>()
                .Children(child => child.Items, grandchildren => grandchildren.RemovedWithJoin<ExternalRemovedViaJoin>()))
            .Nested(model => model.Detail, nested => nested
                .Nested(detail => detail.Detail, inner => inner.RemovedWith<ExternalNestedRemoved>())));
        _result = _projections.GetExternalEventStoreSubscriptions();
    }

    [Fact] void should_subscribe_to_the_external_store() => _result.Single().EventStoreName.ShouldEqual(ExternalStoreName);
    [Fact] void should_include_removal_events_from_each_level() => _result.Single().EventTypeIds.ShouldContainOnly(
        new EventTypeId(nameof(ExternalCreated)),
        new EventTypeId(nameof(ExternalRemoved)),
        new EventTypeId(nameof(ExternalRemovedViaJoin)),
        new EventTypeId(nameof(ExternalNestedRemoved)));

    public record TheReadModel(IEnumerable<Child> Items, OuterDetail Detail);
    public record Child(string Id, IEnumerable<Item> Items);
    public record Item(string Id);
    public record OuterDetail(InnerDetail Detail);
    public record InnerDetail(string Id);
}
