// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_options_change_after_store_construction : given.an_explicit_store
{
    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("captured"));
        _store = CreateStore();
        _options.ExplicitArtifacts.RegisterEventType<AnotherEvent>(_ => _.WithId("later"));
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.DiscoverAll();
        await _store.RegisterAll();
    }

    [Fact] void should_keep_the_original_options_snapshot_on_rediscovery() => _store.EventTypes.AllClrTypes.ShouldContainOnly(typeof(PlainEvent));
    [Fact] void should_register_only_the_captured_recipe() => _payload.Types.Single().Type.Id.ShouldEqual("captured");
}
