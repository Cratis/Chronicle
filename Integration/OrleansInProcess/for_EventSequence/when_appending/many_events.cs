// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace Cratis.Chronicle.Integration.OrleansInProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class many_events(many_events.context fixture, ITestOutputHelper testLogger) : OrleansTest<many_events.context>(fixture)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
        public IList<SomeEvent> Events { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        public override Task Establish()
        {
            Events = [new SomeEvent("some value"), new SomeEvent("some other value"), new SomeEvent("some third value")];
            return Task.CompletedTask;
        }
        public override async Task Because()
        {
            await EventStore.EventLog.AppendMany(EventSourceId, Events);
        }
    }

    [Fact]
    Task should_have_correct_next_sequence_number() => Fixture.ShouldHaveCorrectNextSequenceNumber((ulong)Fixture.Events.Count);

    [Fact]
    Task should_have_correct_tail_sequence_number() => Fixture.ShouldHaveCorrectTailSequenceNumber((ulong)Fixture.Events.Count - 1);

    [Fact]
    async Task should_have_stored_all_the_events_in_correct_order()
    {
        foreach (var (e, i) in Fixture.Events.Select((item, index) => (item, index)))
        {
            testLogger.WriteLine($"Checking stored event {i + 1}");
            await Fixture.ShouldHaveStoredCorrectEvent<SomeEvent>((ulong)i, Fixture.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Fixture.Events[i].Content));
        }
    }
}
