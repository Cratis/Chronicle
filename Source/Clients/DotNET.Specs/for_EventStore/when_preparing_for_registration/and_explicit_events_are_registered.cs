// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_explicit_events_are_registered : given.an_explicit_store
{
    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("alias")).RegisterEventType<Compensation>(_ => _.WithId("compensation"));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.RegisterAll();
    }

    [Fact] void should_resolve_the_explicit_identity() => _store.EventTypes.GetEventTypeFor(typeof(PlainEvent)).ShouldEqual(new EventType("alias", 1));
    [Fact] void should_register_both_types() => _payload.Types.Count().ShouldEqual(2);
    [Fact] void should_register_the_explicit_identity_on_the_wire() => _payload.Types.Select(_ => _.Type.Id).ShouldContainOnly("alias", "compensation");
    [Fact] void should_bind_compensation_metadata_before_registration() => JsonSchema.FromJson(_payload.Types.Single(_ => _.Type.Id == "compensation").Schema).GetCompensationFor().ShouldEqual("alias");
    [Fact] void should_report_success() => _store.Registration.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_extend_native_discovery() => _artifacts.EventTypes.ShouldBeEmpty();
}
