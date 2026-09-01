// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_keyed_children_after_replay.and_a_later_child_is_added.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_keyed_children_after_replay;

[Collection(ChronicleCollection.Name)]
public class and_a_later_child_is_added(context context) : Given<context>(context)
{
    static readonly DateOnly _firstEffectiveDate = new(2026, 10, 1);
    static readonly DateOnly _secondEffectiveDate = new(2026, 11, 1);

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        static readonly Guid _timelineId = Guid.Parse("bc53ea04-8897-4b15-98df-d37f0da4411b");

        public EventSequenceNumber CatchUpSequenceNumber = EventSequenceNumber.Unavailable;
        public Exception? CatchUpError;
        public IEnumerable<FailedPartition> FailedPartitions = [];
        public ProjectionState ObserverState = default!;
        public ReplayCatchUpTimeline Result = default!;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(ReplayCatchUpTimelineActivated),
            typeof(ReplayCatchUpRateAdjusted),
            typeof(ReplayCatchUpScheduledRateCancelled)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ReplayCatchUpTimeline)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<ReplayCatchUpTimeline>();
            var handler = EventStore.Projections.GetAllHandlers().Single(candidate => candidate.Id == projectionId);
            await handler.WaitTillSubscribed();

            var beforeReplay = await EventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(_timelineId, new ReplayCatchUpTimelineActivated(1_000m), Causation.Unknown()),
                new EventForEventSourceId(_timelineId, new ReplayCatchUpRateAdjusted(_firstEffectiveDate, 1_100m), Causation.Unknown()),
                new EventForEventSourceId(_timelineId, new ReplayCatchUpScheduledRateCancelled(_firstEffectiveDate), Causation.Unknown()),
                new EventForEventSourceId(_timelineId, new ReplayCatchUpRateAdjusted(_firstEffectiveDate, 1_100m), Causation.Unknown()),
                new EventForEventSourceId(_timelineId, new ReplayCatchUpTimelineActivated(1_000m), Causation.Unknown())
            ]);
            var lastBeforeReplay = beforeReplay.SequenceNumbers.Last();
            await handler.WaitTillReachesEventSequenceNumber(lastBeforeReplay);

            var replayJobId = await EventStore.Projections.Replay(projectionId);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId);
            await handler.WaitTillSubscribed();
            await handler.WaitTillActive();
            await handler.WaitTillReachesEventSequenceNumber(lastBeforeReplay);

            var catchUp = await EventStore.EventLog.Append(
                _timelineId,
                new ReplayCatchUpRateAdjusted(_secondEffectiveDate, 1_200m));
            CatchUpSequenceNumber = catchUp.SequenceNumber;
            CatchUpError = await Catch.Exception(async () => await handler.WaitTillReachesEventSequenceNumber(CatchUpSequenceNumber));

            ObserverState = await handler.GetState();
            FailedPartitions = await handler.GetFailedPartitions();
            Result = await EventStore.ReadModels.GetInstanceById<ReplayCatchUpTimeline>(_timelineId.ToString());
        }
    }

    [Fact] void should_catch_up_without_error() => Context.CatchUpError.ShouldBeNull();
    [Fact] void should_have_no_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_remain_active() => Context.ObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_advance_the_observer_to_the_later_event() => Context.ObserverState.LastHandledEventSequenceNumber.ShouldEqual(Context.CatchUpSequenceNumber);
    [Fact] void should_advance_the_observer_next_sequence_number() => Context.ObserverState.NextEventSequenceNumber.ShouldEqual(Context.CatchUpSequenceNumber.Next());
    [Fact] void should_advance_the_stored_watermark_to_the_later_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.CatchUpSequenceNumber);
    [Fact] void should_keep_each_keyed_child_exactly_once() => Context.Result.Scheduled.Select(change => change.EffectiveFrom).ShouldContainOnly([_firstEffectiveDate, _secondEffectiveDate]);
    [Fact] void should_keep_the_first_child_rate() => Context.Result.Scheduled.Single(change => change.EffectiveFrom == _firstEffectiveDate).HourlyRate.ShouldEqual(1_100m);
    [Fact] void should_apply_the_later_child_rate() => Context.Result.Scheduled.Single(change => change.EffectiveFrom == _secondEffectiveDate).HourlyRate.ShouldEqual(1_200m);
}

[EventType]
public record ReplayCatchUpTimelineActivated(decimal BaselineRate);

[EventType]
public record ReplayCatchUpRateAdjusted(DateOnly EffectiveFrom, decimal HourlyRate);

[EventType]
public record ReplayCatchUpScheduledRateCancelled(DateOnly EffectiveFrom);

[FromEvent<ReplayCatchUpTimelineActivated>]
public record ReplayCatchUpTimeline(
    Guid Id,
    decimal BaselineRate,
    [ChildrenFrom<ReplayCatchUpRateAdjusted>(key: nameof(ReplayCatchUpRateAdjusted.EffectiveFrom))]
    [RemovedWith<ReplayCatchUpScheduledRateCancelled>(key: nameof(ReplayCatchUpScheduledRateCancelled.EffectiveFrom))]
    IEnumerable<ReplayCatchUpScheduledRate> Scheduled,
    EventSequenceNumber __lastHandledEventSequenceNumber);

public record ReplayCatchUpScheduledRate(DateOnly EffectiveFrom, decimal HourlyRate);

#pragma warning restore SA1402
