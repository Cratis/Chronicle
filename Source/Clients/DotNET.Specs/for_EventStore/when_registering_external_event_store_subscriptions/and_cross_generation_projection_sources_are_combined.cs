// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

using ProjectionCollection = Cratis.Chronicle.Projections.Projections;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_cross_generation_projection_sources_are_combined : Events.for_EventTypes.given.a_cross_generation_registry
{
    readonly List<(EventStoreSubscriptionId Id, string Source, List<EventTypeId> Filters)> _requests = [];
    ProjectionCollection _realProjections;
    IProjections _projections;
    EventStore _store;

    async Task Establish()
    {
        _realProjections = await CreateCombinedProjections();
        _projections = Substitute.For<IProjections>();
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                var filters = new List<EventTypeId>();
                var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
                builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder).AndDoes(configuration => filters.Add(configuration.Arg<EventTypeId>()));
                call.Arg<Action<IEventStoreSubscriptionBuilder>>()(builder);
                _requests.Add((call.Arg<EventStoreSubscriptionId>(), call.Arg<string>(), filters));
                return Task.CompletedTask;
            });
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reactors.Returns([]);
        artifacts.Reducers.Returns([]);
        _store = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        Set("_eventStoreName", new EventStoreName(Consumer));
        Set("_clientArtifactsProvider", artifacts);
        Set("<EventTypes>k__BackingField", _combinedRegistry);
        Set("<Reactors>k__BackingField", Substitute.For<IReactors>());
        Set("<Reducers>k__BackingField", Substitute.For<IReducers>());
        Set("<Projections>k__BackingField", _projections);
        Set("<Subscriptions>k__BackingField", subscriptions);
    }

    async Task Because()
    {
        await _realProjections.Discover();
        var contributions = _realProjections.GetExternalEventStoreSubscriptions().ToArray();
        _projections.GetExternalEventStoreSubscriptions().Returns(contributions);
        await (Task)typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_store, [])!;
    }

    void Set(string field, object value) => typeof(EventStore).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, value);

    [Fact] void should_start_with_all_nine_real_projection_definitions() => _realProjections.Definitions.Count().ShouldEqual(9);
    [Fact] void should_send_exactly_three_requests() => _requests.Count.ShouldEqual(3);
    [Fact] void should_send_exactly_the_inferred_and_explicit_inbox_sources() => _requests.Select(request => request.Source).ShouldContainOnly([PreviousOwner, CurrentOwner, OverrideOwner]);
    [Fact] void should_send_one_historical_source_request() => _requests.Count(request => request.Source == PreviousOwner).ShouldEqual(1);
    [Fact] void should_send_one_current_source_request() => _requests.Count(request => request.Source == CurrentOwner).ShouldEqual(1);
    [Fact] void should_send_one_override_source_request() => _requests.Count(request => request.Source == OverrideOwner).ShouldEqual(1);
    [Fact] void should_derive_the_historical_subscription_id_from_its_source() => _requests.Single(request => request.Source == PreviousOwner).Id.ShouldEqual(new EventStoreSubscriptionId(PreviousOwner));
    [Fact] void should_derive_the_current_subscription_id_from_its_source() => _requests.Single(request => request.Source == CurrentOwner).Id.ShouldEqual(new EventStoreSubscriptionId(CurrentOwner));
    [Fact] void should_derive_the_override_subscription_id_from_its_source() => _requests.Single(request => request.Source == OverrideOwner).Id.ShouldEqual(new EventStoreSubscriptionId(OverrideOwner));
    [Fact] void should_filter_the_historical_subscription_by_only_the_primary_id() => _requests.Single(request => request.Source == PreviousOwner).Filters.ShouldContainOnly([new EventTypeId(EventId)]);
    [Fact] void should_filter_the_current_subscription_by_the_primary_and_join_ids() => _requests.Single(request => request.Source == CurrentOwner).Filters.ShouldContainOnly([new EventTypeId(EventId), new EventTypeId(JoinId)]);
    [Fact] void should_filter_the_override_subscription_by_only_the_primary_id() => _requests.Single(request => request.Source == OverrideOwner).Filters.ShouldContainOnly([new EventTypeId(EventId)]);
    [Fact] void should_not_duplicate_the_historical_filter() => _requests.Single(request => request.Source == PreviousOwner).Filters.Count.ShouldEqual(1);
    [Fact] void should_not_duplicate_the_current_filters_across_definitions_or_clauses() => _requests.Single(request => request.Source == CurrentOwner).Filters.Count.ShouldEqual(2);
    [Fact] void should_not_duplicate_the_override_filter() => _requests.Single(request => request.Source == OverrideOwner).Filters.Count.ShouldEqual(1);
    [Fact] void should_not_send_a_request_for_the_non_inbox_sequence() => _requests.Any(request => request.Source == CustomSequence).ShouldBeFalse();
}
