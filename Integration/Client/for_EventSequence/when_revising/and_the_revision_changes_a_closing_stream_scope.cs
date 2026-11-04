// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.and_the_revision_changes_a_closing_stream_scope.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

[Collection(ChronicleCollection.Name)]
public class and_the_revision_changes_a_closing_stream_scope(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_constrained_event_log(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(PeriodClosed), typeof(PeriodReopened), typeof(PeriodActivity)];
        public override IEnumerable<Type> ConstraintTypes => [typeof(ClosingPeriod)];

        public EventSourceId Source { get; } = "S";
        public IAppendResult OriginalClosing { get; private set; } = null!;
        public EventSequenceNumber OriginalClosingSequenceNumber { get; private set; } = EventSequenceNumber.Unavailable;
        public IAppendResult OriginalReopening { get; private set; } = null!;
        public IAppendResult UnrelatedClosing { get; private set; } = null!;
        public EventSequenceNumber UnrelatedClosingSequenceNumber { get; private set; } = EventSequenceNumber.Unavailable;
        public IAppendResult JanuaryAppend { get; private set; } = null!;
        public IAppendResult FebruaryAppend { get; private set; } = null!;
        public IAppendResult MarchAppend { get; private set; } = null!;
        public bool JanuaryWasClosed { get; private set; }
        public bool FebruaryWasClosed { get; private set; }
        public bool MarchWasClosed { get; private set; }
        public IReadOnlyList<string> ClosuresBeforeRevision { get; private set; } = [];
        public IReadOnlyList<string> ClosuresAfterRevision { get; private set; } = [];
        public IReadOnlyList<string> ClosuresAfterReindex { get; private set; } = [];
        public IReadOnlyList<ClosedStream> ClosedStreams { get; private set; } = [];

        async Task Establish()
        {
            var originalClosing = await EventStore.EventLog.Append(Source, new PeriodClosed("2024-01"));
            OriginalClosing = originalClosing;
            OriginalClosingSequenceNumber = originalClosing.SequenceNumber;
            OriginalClosing.IsSuccess.ShouldBeTrue();
            OriginalReopening = await EventStore.EventLog.Append(Source, new PeriodReopened("2024-02"));
            OriginalReopening.IsSuccess.ShouldBeTrue();
            var unrelatedClosing = await EventStore.EventLog.Append(Source, new PeriodClosed("2024-03"));
            UnrelatedClosing = unrelatedClosing;
            UnrelatedClosingSequenceNumber = unrelatedClosing.SequenceNumber;
            UnrelatedClosing.IsSuccess.ShouldBeTrue();

            JanuaryWasClosed = await EventStore.EventLog.IsStreamCompleted(new(Source, EventStreamId: "2024-01"));
            FebruaryWasClosed = await EventStore.EventLog.IsStreamCompleted(new(Source, EventStreamId: "2024-02"));
            MarchWasClosed = await EventStore.EventLog.IsStreamCompleted(new(Source, EventStreamId: "2024-03"));
            ClosuresBeforeRevision = await Closures(nameof(ClosingPeriod));
        }

        async Task Because()
        {
            await this.ReviseEvent(OriginalClosingSequenceNumber, new PeriodClosed("2024-02"));
            JanuaryAppend = await AppendWhenIndexIsCurrent(() => EventStore.EventLog.Append(Source, new PeriodActivity("January activity"), eventStreamId: "2024-01"));
            FebruaryAppend = await AppendWhenIndexIsCurrent(() => EventStore.EventLog.Append(Source, new PeriodActivity("February activity"), eventStreamId: "2024-02"));
            MarchAppend = await AppendWhenIndexIsCurrent(() => EventStore.EventLog.Append(Source, new PeriodActivity("March activity"), eventStreamId: "2024-03"));
            ClosedStreams = (await EventStore.EventLog.GetClosedStreams()).ToArray();
            ClosuresAfterRevision = await Closures(nameof(ClosingPeriod));

            await Reindex(nameof(ClosingPeriod));
            ClosuresAfterReindex = await Closures(nameof(ClosingPeriod));
        }
    }

    [Fact] void should_accept_the_original_closing_event() => Context.OriginalClosing.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_original_reopening_event() => Context.OriginalReopening.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_unrelated_closing_event() => Context.UnrelatedClosing.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_closed_january_before_revision() => Context.JanuaryWasClosed.ShouldBeTrue();
    [Fact] void should_have_left_february_open_before_revision() => Context.FebruaryWasClosed.ShouldBeFalse();
    [Fact] void should_have_closed_march_before_revision() => Context.MarchWasClosed.ShouldBeTrue();
    [Fact]
    void should_have_indexed_only_the_original_closures_and_their_positions() => Context.ClosuresBeforeRevision.ShouldContainOnly(
        $"source={Context.Source.Value};sourceType=;streamType=;esid=2024-01@{Context.OriginalClosingSequenceNumber.Value}",
        $"source={Context.Source.Value};sourceType=;streamType=;esid=2024-03@{Context.UnrelatedClosingSequenceNumber.Value}");
    [Fact] void should_accept_january_activity_after_revision() => Context.JanuaryAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_february_activity_after_the_later_reopening() => Context.FebruaryAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_still_reject_march_activity() => Context.MarchAppend.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_report_errors_for_the_closed_scope() => Context.MarchAppend.Errors.ShouldBeEmpty();
    [Fact] void should_expose_only_the_unrelated_closure() => Context.ClosedStreams.Single().Scope.ShouldEqual(new ClosedStreamScope(Context.Source, EventStreamId: "2024-03"));
    [Fact] void should_preserve_the_closure_owner() => Context.ClosedStreams.Single().ClosedBy.Value.ShouldEqual(nameof(ClosingPeriod));
    [Fact] void should_preserve_the_unrelated_closing_position() => Context.ClosedStreams.Single().SequenceNumber.ShouldEqual(Context.UnrelatedClosingSequenceNumber);
    [Fact] void should_keep_only_the_unrelated_owned_closure() => Context.ClosuresAfterRevision.ShouldContainOnly($"source={Context.Source.Value};sourceType=;streamType=;esid=2024-03@{Context.UnrelatedClosingSequenceNumber.Value}");
    [Fact] void should_match_a_full_reindex() => Context.ClosuresAfterReindex.ShouldContainOnly(Context.ClosuresAfterRevision);
}
