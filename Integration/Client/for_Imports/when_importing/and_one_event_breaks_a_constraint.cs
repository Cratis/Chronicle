// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Imports;
using context = Cratis.Chronicle.Integration.for_Imports.when_importing.and_one_event_breaks_a_constraint.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_importing;

[Collection(ChronicleCollection.Name)]
public class and_one_event_breaks_a_constraint(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(CustomerEmailImported)];
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueImportedEmail)];

        public ImportResult Result;
        public EventSequenceNumber TailBefore;
        public EventSequenceNumber TailAfter;

        async Task Because()
        {
            TailBefore = await EventStore.EventLog.GetTailSequenceNumber();
            Result = await EventStore.EventLog.Import(Customers.Emails("reject", 300, duplicateAt: 137));
            TailAfter = await EventStore.EventLog.GetTailSequenceNumber();
        }
    }

    [Fact] void should_fail() => Context.Result.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_commit_nothing() => Context.TailAfter.ShouldEqual(Context.TailBefore);
    [Fact] void should_report_the_event_by_its_index() => Context.Result.Rejections.Single().Index.ShouldEqual(137L);
}
