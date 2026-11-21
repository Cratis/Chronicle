// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriptionDurability;

public class when_converting_a_subscription : Specification
{
    static readonly ObserverKey _key = new("observer", "store", "namespace", "log");
    static readonly ConnectedClient _client = new() { ConnectionId = "connection-1", Version = "1.0.0" };

    ObserverDurableSubscription _clientReactor;
    ObserverDurableSubscription _projection;
    ObserverDurableSubscription _unsubscribed;

    void Because()
    {
        var silo = SiloAddress.Zero;
        _clientReactor = new ObserverSubscription(_key.ObserverId, _key, [], typeof(IReactorPageObserverSubscriber), silo, _client, true, new ObserverFilters(["tag"], new EventSourceType("source"), EventStreamType.All))
        {
            Targets = [new(silo, _client)]
        }.ToDurable();
        _projection = new ObserverSubscription(_key.ObserverId, _key, [], typeof(IProjectionObserverSubscriber), silo).ToDurable();
        _unsubscribed = ObserverSubscription.Unsubscribed.ToDurable();
    }

    [Fact] void should_record_the_kind_of_a_client_reactor() => _clientReactor.Kind.ShouldEqual(ObserverSubscriptionKind.ClientReactor);
    [Fact] void should_record_that_a_client_reactor_delivers_pages() => _clientReactor.DeliversPages.ShouldBeTrue();
    [Fact] void should_record_the_filter_tags() => _clientReactor.Filters.Tags.Single().ShouldEqual("tag");
    [Fact] void should_record_the_connection_of_each_target() => _clientReactor.Targets.Single().ConnectionId.ShouldEqual("connection-1");
    [Fact] void should_record_the_silo_of_each_target() => _clientReactor.Targets.Single().Silo.ShouldEqual(SiloAddress.Zero.ToParsableString());
    [Fact] void should_record_the_kind_of_a_projection() => _projection.Kind.ShouldEqual(ObserverSubscriptionKind.Projection);
    [Fact] void should_record_no_target_for_a_kernel_owned_subscription() => _projection.Targets.ShouldBeEmpty();
    [Fact] void should_record_no_filters_when_there_are_none() => _projection.Filters.ShouldBeNull();
    [Fact] void should_record_nothing_for_an_unsubscribed_observer() => _unsubscribed.ShouldBeNull();
}
