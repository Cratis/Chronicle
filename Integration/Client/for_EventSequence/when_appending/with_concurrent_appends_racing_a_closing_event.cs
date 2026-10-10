// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_concurrent_appends_racing_a_closing_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_concurrent_appends_racing_a_closing_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public IAppendResult Closing;
        public EventSequenceNumber ClosingNumber;
        public int StoredCount;
        public IAppendResult[] Concurrent;
        public IAppendResult AfterClose;
        public bool HasCoveredFactAfterClosing;

        async Task Because()
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var appends = Enumerable.Range(0, 20).Select(async index =>
            {
                await start.Task;
                return await Append(new given.StreamActivity($"racing {index}"));
            }).ToArray();
            var closing = Task.Run(async () =>
            {
                await start.Task;
                var result = await EventStore.EventLog.Append(Source, new given.StreamClosed(), eventStreamType: StreamType, eventStreamId: StreamId);
                ClosingNumber = result.SequenceNumber;
                return (IAppendResult)result;
            });
            start.SetResult();
            Concurrent = await Task.WhenAll(appends).WaitAsync(TimeSpan.FromSeconds(30));
            Closing = await closing.WaitAsync(TimeSpan.FromSeconds(30));
            AfterClose = await Append(new given.StreamActivity("after close"));
            var stored = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, Source);
            StoredCount = stored.Count;
            HasCoveredFactAfterClosing = stored.Any(@event => @event.Context.SequenceNumber > ClosingNumber);
        }
    }

    [Fact] void should_commit_the_closing_fact() => Context.Closing.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_successful_appends_and_the_closing_fact() => Context.StoredCount.ShouldEqual(Context.Concurrent.Count(result => result.IsSuccess) + 1);
    [Fact] void should_store_no_covered_fact_after_the_closing_fact() => Context.HasCoveredFactAfterClosing.ShouldBeFalse();
    [Fact] void should_refuse_a_subsequent_covered_fact() => Context.AfterClose.HasConstraintViolations.ShouldBeTrue();
}
