// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_implicit_after_a_concurrent_append.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork;

[Collection(ChronicleCollection.Name)]
public class when_committing_implicit_after_a_concurrent_append(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public bool Accepted;
        public EventSourceId[] ViolationSources;
        public int StoredCount;
        public EventSourceId Source = EventSourceId.New();
        public override IEnumerable<Type> EventTypes => [typeof(given.ImplicitScopeEvent)];

        async Task Because()
        {
            await EventStore.EventLog.Append(Source, new given.ImplicitScopeEvent("first"));
            var boundary = await EventStore.EventLog.GetTailSequenceNumber();
            using var unit = new UnitOfWork(CorrelationId.New(), _ => { }, EventStore);
            unit.AddImplicitScope(EventSequenceId.Log, Source, boundary);
            unit.AddEvent(EventSequenceId.Log, Source, new given.ImplicitScopeEvent("planned"), Causation.Unknown());
            await EventStore.EventLog.Append(Source, new given.ImplicitScopeEvent("concurrent"));
            await unit.Commit();
            Accepted = unit.IsSuccess;
            ViolationSources = unit.GetConcurrencyViolations().Select(_ => _.EventSourceId).ToArray();
            StoredCount = (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, Source)).Count;
        }
    }

    [Fact] void should_return_a_concurrency_violation() => Context.Accepted.ShouldBeFalse();
    [Fact] void should_identify_the_guarded_source() => Context.ViolationSources.Single().ShouldEqual(Context.Source);
    [Fact] void should_not_store_the_stale_write() => Context.StoredCount.ShouldEqual(2);
}
