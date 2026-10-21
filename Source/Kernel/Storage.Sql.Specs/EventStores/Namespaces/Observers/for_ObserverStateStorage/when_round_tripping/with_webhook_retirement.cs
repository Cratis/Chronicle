// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_webhook_retirement : given.an_observer_state_storage
{
    KernelObserverState _expected;
    KernelObserverState _restored;
    KernelObserverState _legacy;
    KernelObserverState _cleared;

    async Task Because()
    {
        _expected = new()
        {
            Identifier = "observer",
            WebhookRetirement = new(Guid.NewGuid(), new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero), "https://receiver.example:8443"),
            WebhookRegistrationId = Guid.NewGuid(),
            WebhookRegistrationFingerprint = "opaque-definition",
            WebhookReplayRetained = true
        };
        await _storage.Save(new() { Identifier = "legacy" });
        await _storage.Save(_expected);
        _restored = await _storage.Get("observer");
        _legacy = await _storage.Get("legacy");
        await _storage.Save(_expected with { WebhookRetirement = null, WebhookRegistrationId = null, WebhookRegistrationFingerprint = null, WebhookReplayRetained = false });
        _cleared = await _storage.Get("observer");
    }

    [Fact] void should_keep_the_retained_replay_marker() => _restored.WebhookReplayRetained.ShouldBeTrue();
    [Fact] void should_not_resume_legacy_replays() => _legacy.WebhookReplayRetained.ShouldBeFalse();
    [Fact] void should_keep_the_reason() => _restored.WebhookRetirement.ShouldEqual(_expected.WebhookRetirement);
    [Fact] void should_keep_the_registration_intent() => _restored.WebhookRegistrationId.ShouldEqual(_expected.WebhookRegistrationId);
    [Fact] void should_keep_the_registration_fingerprint() => _restored.WebhookRegistrationFingerprint.ShouldEqual(_expected.WebhookRegistrationFingerprint);
    [Fact] void should_not_retire_legacy_observers() => _legacy.WebhookRetirement.ShouldBeNull();
    [Fact] void should_not_authorize_legacy_registration() => _legacy.WebhookRegistrationId.ShouldBeNull();
    [Fact] void should_clear_the_retained_replay_marker() => _cleared.WebhookReplayRetained.ShouldBeFalse();
    [Fact] void should_clear_the_retirement() => _cleared.WebhookRetirement.ShouldBeNull();
    [Fact] void should_clear_the_registration_intent() => _cleared.WebhookRegistrationId.ShouldBeNull();
}
