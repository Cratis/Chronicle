// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_revision_of_an_event_before_the_boundary.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_revision_of_an_event_before_the_boundary(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        public EventSequenceNumber TailAfterRevision;

        async Task Because()
        {
            var first = await EventStore.EventLog.Append(Source, new given.StockSet(1));
            CreateUnit();
            using var frame = Unit.EnforceReadProtection();
            await Acquire();

            // #4900 is deliberately excluded: a revision changes history without advancing the append tail.
            await Independent(() => EventStore.EventLog.Revise(first.SequenceNumber, new given.StockSet(0)));
            using (var reads = Unit.AllowReads(ReadAllowance.Advisory, "document the history-mutation exclusion"))
            {
                TailAfterRevision = await EventStore.EventLog.GetTailSequenceNumber();
            }
            Stage();
            await Commit();
        }
    }

    [Fact] void should_preserve_the_original_read_value() => Context.Read.Instance!.Quantity.ShouldEqual(1);
    [Fact] void should_not_advance_the_append_tail_for_a_revision() => Context.TailAfterRevision.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_document_that_read_protection_does_not_detect_history_mutations() => Context.Unit.IsSuccess.ShouldBeTrue();
}
