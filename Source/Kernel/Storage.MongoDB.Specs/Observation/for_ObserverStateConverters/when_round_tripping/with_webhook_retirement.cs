// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_webhook_retirement : Specification
{
    KernelObserverState _expected;
    KernelObserverState _restored;
    KernelObserverState _legacy;

    void Because()
    {
        _expected = new()
        {
            Identifier = "observer",
            WebhookRetirement = new(Guid.NewGuid(), new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero), "https://receiver.example:8443"),
            WebhookRegistrationId = Guid.NewGuid(),
            WebhookRegistrationFingerprint = "opaque-definition",
            WebhookReplayRetained = true
        };
        _restored = BsonSerializer.Deserialize<ObserverState>(_expected.ToMongoDB().ToBson()).ToKernel();
        var legacy = new KernelObserverState { Identifier = "legacy" }.ToMongoDB().ToBsonDocument();
        var map = BsonClassMap.LookupClassMap(typeof(ObserverState));
        legacy.Remove(map.GetMemberMap(nameof(ObserverState.WebhookRetirement)).ElementName);
        legacy.Remove(map.GetMemberMap(nameof(ObserverState.WebhookRegistrationId)).ElementName);
        legacy.Remove(map.GetMemberMap(nameof(ObserverState.WebhookRegistrationFingerprint)).ElementName);
        legacy.Remove(map.GetMemberMap(nameof(ObserverState.WebhookReplayRetained)).ElementName);
        _legacy = BsonSerializer.Deserialize<ObserverState>(legacy).ToKernel();
    }

    [Fact] void should_keep_the_retained_replay_marker() => _restored.WebhookReplayRetained.ShouldBeTrue();
    [Fact] void should_not_resume_legacy_replays() => _legacy.WebhookReplayRetained.ShouldBeFalse();
    [Fact] void should_keep_the_reason() => _restored.WebhookRetirement.ShouldEqual(_expected.WebhookRetirement);
    [Fact] void should_keep_the_registration_intent() => _restored.WebhookRegistrationId.ShouldEqual(_expected.WebhookRegistrationId);
    [Fact] void should_keep_the_registration_fingerprint() => _restored.WebhookRegistrationFingerprint.ShouldEqual(_expected.WebhookRegistrationFingerprint);
    [Fact] void should_not_retire_legacy_observers() => _legacy.WebhookRetirement.ShouldBeNull();
    [Fact] void should_not_authorize_legacy_registration() => _legacy.WebhookRegistrationId.ShouldBeNull();
}
