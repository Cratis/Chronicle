// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.ReadModels;

using context = Cratis.Chronicle.Integration.for_ReadModels.when_reading_for_a_decision_scoped_to_a_stream.context;

namespace Cratis.Chronicle.Integration.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_reading_for_a_decision_scoped_to_a_stream(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public DecisionRead<DecisionState> Read;
        public bool OtherStreamAccepted;
        public bool SameStreamConflicted;

        public override IEnumerable<Type> EventTypes => [typeof(DecisionCreated)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(DecisionState)];

        async Task Because()
        {
            var source = Guid.NewGuid().ToString("D");
            await EventStore.EventLog.Append(source, new DecisionCreated("Selected"), "decisions", "selected");
            await EventStore.EventLog.Append(source, new DecisionCreated("Other"), "decisions", "other");
            Read = await EventStore.GetDecisionReads().GetDetached<DecisionState>(source, "decisions", "selected");
            await EventStore.EventLog.Append(source, new DecisionCreated("Concurrent other"), "decisions", "other");
            OtherStreamAccepted = (await EventStore.EventLog.AppendMany([], guardedBy: [Read])).IsSuccess;
            await EventStore.EventLog.Append(source, new DecisionCreated("Concurrent selected"), "decisions", "selected");
            SameStreamConflicted = (await EventStore.EventLog.AppendMany([], guardedBy: [Read])).ConcurrencyViolations.Any();
        }
    }

    [Fact] void should_fold_only_the_selected_stream() => Context.Read.Instance!.Name.ShouldEqual("Selected");
    [Fact] void should_accept_a_change_on_another_stream() => Context.OtherStreamAccepted.ShouldBeTrue();
    [Fact] void should_conflict_with_a_change_on_the_same_stream() => Context.SameStreamConflicted.ShouldBeTrue();
}

#pragma warning restore SA1402
