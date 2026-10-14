// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection.with_a_revision_to_another_generation.context;
using stock = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

namespace Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection;

[Collection(ChronicleCollection.Name)]
public class with_a_revision_to_another_generation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_reducer_decision_scenario(fixture)
    {
        public Exception Error;
        public DecisionRead<given.Stock>? Read;

        async Task Because()
        {
            var appended = await EventStore.EventLog.Append(Source, new stock.StockSet(10));
            await EventStore.EventLog.Revise(appended.SequenceNumber, new stock.StockSetV2(0));
            await WaitForRevision(appended.SequenceNumber);
            CreateUnit();
            using var frame = Unit.EnforceReadProtection();
            Error = await Catch.Exception(async () => Read = await EventStore.GetDecisionReads().GetFor<given.Stock>(Unit, Source));
        }

        async Task WaitForRevision(EventSequenceNumber sequenceNumber)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                while (true)
                {
                    var events = await EventStore.EventLog.GetFromSequenceNumber(sequenceNumber, Source).WaitAsync(timeout.Token);
                    var storedEvent = events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber);

                    // Migrated generation 2 content can already exist before the revision is applied.
                    if (storedEvent?.Context.EventType.Generation.Value == 2 && storedEvent.Content is stock.StockSetV2 { Quantity: 0 }) return;
                    await Task.Delay(50, timeout.Token);
                }
            }
            catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Revision of event {sequenceNumber} was not visible with generation 2 and quantity 0 within 30 seconds.", error);
            }
        }
    }

    [Fact] void should_refuse_instead_of_using_stale_generational_content() => ((DecisionReadRefused)Context.Error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerGeneration);
    [Fact] void should_not_expose_the_model() => Context.Read.ShouldBeNull();
    [Fact] void should_not_enroll_a_partial_fold() => Context.Unit.HasEnrolledDecisionReads.ShouldBeFalse();
}
