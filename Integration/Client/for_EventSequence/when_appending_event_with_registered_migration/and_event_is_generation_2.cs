// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration.and_event_is_generation_2.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration;

[Collection(ChronicleCollection.Name)]
public class and_event_is_generation_2(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EmployeeRegisteredV1), typeof(EmployeeRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(EmployeeRegisteredMigrator)];

        public EventSourceId EventSourceId { get; } = "some-employee";
        public EmployeeRegistered Event { get; private set; }
        public IAppendResult AppendResult { get; private set; }

        void Establish()
        {
            Event = new EmployeeRegistered("Jane", "Smith");
        }

        async Task Because()
        {
            AppendResult = await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] void should_succeed() => Context.AppendResult.IsSuccess.ShouldBeTrue();
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);
    [Fact] Task should_have_combined_first_and_last_name_into_generation_1_content() => Context.ShouldHaveAppendedEvent<EmployeeRegistered>(0, Context.EventSourceId.Value, e => e.FirstName.ShouldEqual("Jane"));
}
