// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration.and_event_has_default_value.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration;

[Collection(ChronicleCollection.Name)]
public class and_event_has_default_value(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ContractSigned)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(ContractSignedMigrator)];

        public EventSourceId EventSourceId { get; } = "some-contract";
        public ContractSigned Event { get; private set; }
        public IAppendResult AppendResult { get; private set; }

        void Establish()
        {
            Event = new ContractSigned("c-001");
        }

        async Task Because()
        {
            AppendResult = await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] void should_succeed() => Context.AppendResult.IsSuccess.ShouldBeTrue();
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);
    [Fact] Task should_have_stored_contract_id() => Context.ShouldHaveAppendedEvent<ContractSigned>(0, Context.EventSourceId.Value, e => e.ContractId.ShouldEqual("c-001"));
}
