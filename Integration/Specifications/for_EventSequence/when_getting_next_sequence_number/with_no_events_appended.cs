// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_getting_next_sequence_number.with_no_events_appended.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_getting_next_sequence_number;

[Collection(ChronicleCollection.Name)]
public class with_no_events_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSequenceNumber NextSequenceNumber { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            NextSequenceNumber = await EventStore.EventLog.GetNextSequenceNumber();
        }
    }

    [Fact]
    void should_return_the_first_sequence_number() => Context.NextSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
