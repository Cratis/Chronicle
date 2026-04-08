// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_committed_via_a_unit_of_work.and_collecting_the_appended_events.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection.when_committed_via_a_unit_of_work;

[Collection(ChronicleCollection.Name)]
public class and_collecting_the_appended_events(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.an_event_append_collection_scope(fixture)
    {
        async Task Because()
        {
            AppendedEventsCollector = StartCollectingAppends();

            var unitOfWork = EventStore.UnitOfWorkManager.Begin(CorrelationId.New());
            await EventStore.EventLog.Transactional.Append(EventSourceId, new AnEventHappened(42));
            await unitOfWork.Commit();
        }
    }

    [Fact] void should_collect_one_event() => Context.AppendedEventsCollector.All.Count.ShouldEqual(1);
    [Fact] void should_have_appended_the_event() => Context.AppendedEventsCollector.All[0].Event.Content.ShouldBeOfExactType<AnEventHappened>();
    [Fact] void should_be_successful() => Context.AppendedEventsCollector.All[0].Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() => Context.AppendedEventsCollector.All[0].Result.SequenceNumber.IsActualValue.ShouldBeTrue();
    [Fact] void should_be_for_the_correct_event_source() => Context.AppendedEventsCollector.All[0].Event.Context.EventSourceId.ShouldEqual(Context.EventSourceId);
}
