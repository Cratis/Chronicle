// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_webhook_probe : given.an_observer_state_storage
{
    KernelObserverState _expected;
    KernelObserverState _restored;
    KernelObserverState _legacy;

    async Task Establish()
    {
        await _storage.Save(new() { Identifier = "legacy" });
    }

    async Task Because()
    {
        _expected = new()
        {
            Identifier = "observer",
            WebhookProbe = new(Guid.NewGuid(), Guid.NewGuid(), new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero), 3)
            {
                EpisodeId = new(Guid.NewGuid()),
                Position = new(42),
                Partition = "partition",
                DrainPending = true,
                Cancelled = true,
                ResumePending = true,
                ResumePosition = new(43),
                DefinitionFingerprint = "opaque-hash",
                PartitionCounts = [new("event-a", 7UL)]
            }
        };
        await _storage.Save(_expected);
        _restored = await _storage.Get("observer");
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_generation() => _restored.WebhookProbe.Id.ShouldEqual(_expected.WebhookProbe.Id);
    [Fact] void should_keep_the_sampled_deadline() => _restored.WebhookProbe.Due.ShouldEqual(_expected.WebhookProbe.Due);
    [Fact] void should_keep_the_claimed_cursor() => _restored.WebhookProbe.Position.ShouldEqual(_expected.WebhookProbe.Position);
    [Fact] void should_keep_success_without_redelivery() => _restored.WebhookProbe.ResumePending.ShouldBeTrue();
    [Fact] void should_keep_the_next_undelivered_event() => _restored.WebhookProbe.ResumePosition.ShouldEqual(_expected.WebhookProbe.ResumePosition);
    [Fact] void should_keep_absolute_counts_for_idempotent_recovery() => _restored.WebhookProbe.PartitionCounts.Single().Count.Value.ShouldEqual(7UL);
    [Fact] void should_not_authorize_legacy_quarantine() => _legacy.WebhookProbe.ShouldBeNull();
    [Fact] void should_keep_the_stable_pending_subscriber() => _restored.WebhookProbe.Partition.ShouldEqual(_expected.WebhookProbe.Partition);
    [Fact] void should_keep_the_unacknowledged_barrier() => _restored.WebhookProbe.DrainPending.ShouldBeTrue();
    [Fact] void should_keep_operator_invalidation() => _restored.WebhookProbe.Cancelled.ShouldBeTrue();
}
