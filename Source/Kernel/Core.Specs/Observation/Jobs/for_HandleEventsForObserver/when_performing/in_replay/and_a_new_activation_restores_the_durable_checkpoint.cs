// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

/// <summary>
/// Unit restoration into a fresh TestKit instance, not process death or Orleans storage reloading.
/// </summary>
public class and_a_new_activation_restores_the_durable_checkpoint : given.a_restorable_replay_job_step
{
    readonly SortedDictionary<ulong, string> _sink = [];
    readonly SortedDictionary<ulong, string> _uninterruptedSink = [];
    Activation _original;
    Activation _restored;
    Activation _uninterrupted;
    HandleEventsForObserverState _checkpoint;
    byte[] _durableBytes;
    int _writesBeforeInterruption;
    int _writesAfterInterruption;
    bool _originalTerminated;

    async Task Because()
    {
        _original = await CreateActivation(InitialState(), _sink, interrupt: true);
        var interruptedWork = _original.Step.InvokePerformStep(_original.State);
        await _original.AtInterruption.Task;
        _durableBytes = _original.Snapshots[^1].ToArray();
        _writesBeforeInterruption = _original.Snapshots.Count;
        _original.Step.Run!.Close();
        _original.ReleaseInterruption.SetResult();
        await interruptedWork;
        _originalTerminated = interruptedWork.IsCompletedSuccessfully;
        _writesAfterInterruption = _original.Snapshots.Count;

        // Only bytes and the external idempotent sink cross this boundary; no original dependencies are reused.
        _checkpoint = Restore(_durableBytes);
        _restored = await CreateActivation(Restore(_durableBytes), _sink);
        await _restored.Step.InvokePerformStep(_restored.State);
        _uninterrupted = await CreateActivation(InitialState(), _uninterruptedSink);
        await _uninterrupted.Step.InvokePerformStep(_uninterrupted.State);
    }

    static string CanonicalHash(SortedDictionary<ulong, string> sink) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', sink.Values))));

    [Fact] void should_terminate_the_interrupted_work_before_restoring() => _originalTerminated.ShouldBeTrue();
    [Fact] void should_have_successful_durable_snapshots() => _writesBeforeInterruption.ShouldBeGreaterThan(0);
    [Fact] void should_not_persist_after_closing_admission() => _writesAfterInterruption.ShouldEqual(_writesBeforeInterruption);
    [Fact] void should_have_delivered_the_original_prefix_and_unconfirmed_suffix() => string.Join(',', _original.Deliveries).ShouldEqual("1,2,3,4,5");
    [Fact] void should_restore_only_the_confirmed_prefix() => _checkpoint.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_leave_the_old_activation_at_the_confirmed_prefix() => _original.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_preserve_the_observer_identity() => _checkpoint.ObserverKey.ShouldEqual(InitialState().ObserverKey);
    [Fact] void should_preserve_the_requested_start() => _checkpoint.StartEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_preserve_the_requested_end() => _checkpoint.EndEventSequenceNumber.ShouldEqual((EventSequenceNumber)8UL);
    [Fact] void should_preserve_replay_mode() => _checkpoint.EventObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] void should_preserve_the_reader_scope() => _checkpoint.ReaderScope!.IsWildcard.ShouldBeTrue();
    [Fact] void should_preserve_the_scope_event_types() => _checkpoint.ReaderScope!.EventTypes.ShouldBeEmpty();
    [Fact] void should_start_the_original_range_at_one() => _original.RequestedStart.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_request_the_production_resume_bound() => _restored.RequestedStart.ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_deliver_the_range_selected_by_that_bound() => string.Join(',', _restored.Deliveries).ShouldEqual("4,5,6,7,8");
    [Fact] void should_redeliver_exactly_the_unconfirmed_suffix() => string.Join(',', _original.Deliveries.Intersect(_restored.Deliveries)).ShouldEqual("4,5");
    [Fact] void should_bound_redelivery_below_the_checkpoint_interval() => _original.Deliveries.Intersect(_restored.Deliveries).Count().ShouldBeLessThan(3);
    [Fact] void should_not_omit_any_sequence() => string.Join(',', _original.Deliveries.Union(_restored.Deliveries).Order()).ShouldEqual("1,2,3,4,5,6,7,8");
    [Fact] void should_end_with_eight_durable() => Restore(_restored.Snapshots[^1]).LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)8UL);
    [Fact] void should_use_a_different_silo() => ReferenceEquals(_original.Silo, _restored.Silo).ShouldBeFalse();
    [Fact] void should_use_a_different_grain() => ReferenceEquals(_original.Step, _restored.Step).ShouldBeFalse();
    [Fact] void should_use_a_different_state() => ReferenceEquals(_original.State, _restored.State).ShouldBeFalse();
    [Fact] void should_not_alias_the_deserialized_checkpoint() => ReferenceEquals(_checkpoint, _restored.State).ShouldBeFalse();
    [Fact] void should_keep_the_saved_bytes_unchanged_by_restored_writes() => JsonSerializer.SerializeToUtf8Bytes(_checkpoint, SerializerOptions).SequenceEqual(_durableBytes).ShouldBeTrue();
    [Fact] void should_deliver_every_sequence_in_the_independent_uninterrupted_run() => string.Join(',', _uninterrupted.Deliveries).ShouldEqual("1,2,3,4,5,6,7,8");
    [Fact] void should_have_eight_canonical_sink_entries() => _sink.Count.ShouldEqual(8);
    [Fact] void should_match_the_uninterrupted_canonical_count() => _sink.Count.ShouldEqual(_uninterruptedSink.Count);
    [Fact] void should_match_the_uninterrupted_canonical_hash() => CanonicalHash(_sink).ShouldEqual(CanonicalHash(_uninterruptedSink));
}
