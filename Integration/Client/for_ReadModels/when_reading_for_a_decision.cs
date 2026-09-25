// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_reading_for_a_decision.context;

namespace Cratis.Chronicle.Integration.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_reading_for_a_decision(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(DecisionCreated), typeof(DecisionRemoved), typeof(DecisionChanged)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(DecisionModel)];

        public ReadModelInstance<DecisionModel> Created;
        public ReadModelInstance<DecisionModel> Removed;
        public ReadModelInstance<DecisionModel> NeverCreated;
        public IAppendResult StaleAppend;

        async Task Because()
        {
            var createdId = Guid.NewGuid().ToString();
            var removedId = Guid.NewGuid().ToString();
            await EventStore.EventLog.Append(createdId, new DecisionCreated("first"));
            Created = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(createdId);
            await EventStore.EventLog.Append(createdId, new DecisionCreated("second"));
            StaleAppend = await EventStore.EventLog.AppendMany(
                [new EventForEventSourceId(Guid.NewGuid().ToString(), new DecisionChanged())],
                concurrencyScopes: Created.ToConcurrencyScopes());

            await EventStore.EventLog.Append(removedId, new DecisionCreated("temporary"));
            await EventStore.EventLog.Append(removedId, new DecisionRemoved());
            Removed = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(removedId);
            NeverCreated = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(Guid.NewGuid().ToString());
        }
    }

    [Fact] void should_fold_the_created_instance() => Context.Created.Instance!.Name.ShouldEqual("first");
    [Fact] void should_reject_a_stale_decision_appended_to_another_source() => Context.StaleAppend.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_report_removed_as_absent_with_a_watermark() => Context.Removed.Instance.ShouldBeNull();
    [Fact] void should_keep_the_removal_watermark() => Context.Removed.SequenceNumber.IsActualValue.ShouldBeTrue();
    [Fact] void should_report_never_created_as_absent() => Context.NeverCreated.Instance.ShouldBeNull();
    [Fact] void should_guard_never_created_as_before_first() => Context.NeverCreated.ToConcurrencyScopes().Values.Single().ExpectsNoMatchingEvent.ShouldBeTrue();
}

[EventType]
public record DecisionCreated(string Name);

[EventType]
public record DecisionRemoved;

[EventType]
public record DecisionChanged;

[FromEvent<DecisionCreated>]
[RemovedWith<DecisionRemoved>]
public record DecisionModel(Guid Id, string Name);

#pragma warning restore SA1402
