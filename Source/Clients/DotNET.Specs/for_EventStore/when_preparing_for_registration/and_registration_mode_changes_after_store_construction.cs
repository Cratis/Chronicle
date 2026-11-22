// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_registration_mode_changes_after_store_construction : given.an_explicit_store
{
    void Establish()
    {
        _options.RegistrationMode = RegistrationMode.EventTypesOnly;
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("captured"));
        _store = CreateStore();
        _options.RegistrationMode = RegistrationMode.Inline;
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.RegisterAll();
        await _store.RegisterAll();
    }

    [Fact] void should_keep_captured_mode() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_still_register_event_catalog() => _payload.Types.Single().Type.Id.ShouldEqual("captured");
    [Fact] void should_keep_local_identity() => _store.EventTypes.GetEventTypeFor(typeof(PlainEvent)).Id.Value.ShouldEqual("captured");
}
