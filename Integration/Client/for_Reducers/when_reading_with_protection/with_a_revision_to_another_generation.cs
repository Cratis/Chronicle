// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            CreateUnit();
            using var frame = Unit.EnforceReadProtection();
            Error = await Catch.Exception(async () => Read = await EventStore.GetDecisionReads().GetFor<given.Stock>(Unit, Source));
        }
    }

    [Fact] void should_refuse_instead_of_using_stale_generational_content() => ((DecisionReadRefused)Context.Error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerGeneration);
    [Fact] void should_not_expose_the_model() => Context.Read.ShouldBeNull();
    [Fact] void should_not_enroll_a_partial_fold() => Context.Unit.HasEnrolledDecisionReads.ShouldBeFalse();
}
