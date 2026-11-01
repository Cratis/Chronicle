// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_handing_over;

public class with_the_expected_owner : Specification
{
    ObserverStateStorage _storage;
    ObserverState _original;
    ObserverState _result;
    ObserverSequenceCheckpoint _target;
    bool _accepted;

    async Task Establish()
    {
        _storage = new();
        _original = new()
        {
            Identifier = "observer",
            RunningState = ObserverRunningState.Suspended,
            SubscribesToAllEvents = true,
            NextEventSequenceNumber = new(999),
            ReplayingPartitions = ["replaying"],
            PartitionReplayCompletions = [new("replaying", new(10), new(20), null, DateTimeOffset.UnixEpoch)],
            CatchingUpPartitions = ["catching-up"],
            InFlightPartitions = ["in-flight"],
            OwnershipRevision = new(19),
            AlertLifecycleId = Guid.NewGuid(),
            AlertRevision = 4,
            AlertDisposition = AlertDisposition.Retired,
            QuarantineEpisodeId = Guid.NewGuid(),
            Initialization = new(ObserverInitializationState.Initialized, []),
            TypeSetControl = new(new(3), new("reactor", ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember>(), []),
            HandledEventCount = new(123),
            HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { [new("event")] = new(123) },
            WebhookProbe = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, 2)
        };
        await _storage.Save(_original, "A");
        _target = ObserverSequenceCheckpoint.Fresh("observer", "B") with
        {
            NextEventSequenceNumber = new(43),
            LastHandledEventSequenceNumber = new(42),
            TailEventSequenceNumber = new(80),
            FailedPartitionCount = new(2),
            IsReplaying = true,
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch.AddMinutes(5), [])]
        };
    }

    async Task Because()
    {
        _accepted = await _storage.HandOver("observer", "A", _target);
        _result = await _storage.Get("observer");
    }

    [Fact] void should_accept_the_handover() => _accepted.ShouldBeTrue();
    [Fact] void should_change_the_owner() => _result.Owner.ShouldEqual(_target.EventSequenceId);
    [Fact] void should_restore_the_next_position() => _result.NextEventSequenceNumber.ShouldEqual(_target.NextEventSequenceNumber);
    [Fact] void should_restore_the_last_position() => _result.LastHandledEventSequenceNumber.ShouldEqual(_target.LastHandledEventSequenceNumber);
    [Fact] void should_restore_the_tail() => _result.TailEventSequenceNumber.ShouldEqual(_target.TailEventSequenceNumber);
    [Fact] void should_restore_the_failed_count() => _result.FailedPartitionCount.ShouldEqual(_target.FailedPartitionCount);
    [Fact] void should_restore_replay() => _result.IsReplaying.ShouldBeTrue();
    [Fact] void should_restore_partition_claims() => _result.PartitionClaims.ShouldEqual(_target.PartitionClaims);
    [Fact] void should_restore_the_wide_claim() => _result.ObserverWideClaim.ShouldEqual(_target.ObserverWideClaim);
    [Fact] void should_restore_delivery_fences() => _result.DeliveryFences.ShouldEqual(_target.DeliveryFences);
    [Fact] void should_clear_replaying_partitions() => _result.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_clear_partition_replay_completions() => _result.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_clear_catching_up_partitions() => _result.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_clear_in_flight_partitions() => _result.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_preserve_the_running_state() => _result.RunningState.ShouldEqual(_original.RunningState);
    [Fact] void should_preserve_the_subscription_mode() => _result.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_preserve_the_ownership_revision() => _result.OwnershipRevision.ShouldEqual(_original.OwnershipRevision);
    [Fact] void should_preserve_the_lifecycle() => _result.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_preserve_the_alert_revision() => _result.AlertRevision.ShouldEqual(_original.AlertRevision);
    [Fact] void should_preserve_the_alert_disposition() => _result.AlertDisposition.ShouldEqual(_original.AlertDisposition);
    [Fact] void should_preserve_the_quarantine() => _result.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
    [Fact] void should_preserve_initialization() => _result.Initialization.ShouldEqual(_original.Initialization);
    [Fact] void should_preserve_type_set_control() => _result.TypeSetControl.ShouldEqual(_original.TypeSetControl);
    [Fact] void should_preserve_handled_counts() => _result.HandledEventCount.ShouldEqual(_original.HandledEventCount);
    [Fact] void should_preserve_type_counts() => _result.HandledEventCountPerEventType.ShouldEqual(_original.HandledEventCountPerEventType);
    [Fact] void should_preserve_webhook_probe() => _result.WebhookProbe.ShouldEqual(_original.WebhookProbe);

    void Destroy() => _storage.Dispose();
}
