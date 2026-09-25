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
        public IAppendResult FreshAppend;
        public IAppendResult FirstMatchingAppend;
        public IAppendResult AbsentStaleAppend;
        public EventSourceId CreatedId;
        public EventSequenceNumber RemovalSequenceNumber;

        async Task Because()
        {
            CreatedId = Guid.NewGuid().ToString();
            var removedId = Guid.NewGuid().ToString();
            var neverCreatedId = Guid.NewGuid().ToString();
            await EventStore.EventLog.Append(CreatedId, new DecisionCreated("first"));
            Created = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(CreatedId);
            await EventStore.EventLog.Append(CreatedId, new DecisionCreated("second"));
            StaleAppend = await EventStore.EventLog.AppendMany(
                [new EventForEventSourceId(Guid.NewGuid().ToString(), new DecisionChanged())],
                concurrencyScopes: Created.ToConcurrencyScopes());
            var fresh = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(CreatedId);
            FreshAppend = await EventStore.EventLog.AppendMany(
                [new EventForEventSourceId(Guid.NewGuid().ToString(), new DecisionChanged())],
                concurrencyScopes: fresh.ToConcurrencyScopes());

            await EventStore.EventLog.Append(removedId, new DecisionCreated("temporary"));
            RemovalSequenceNumber = (await EventStore.EventLog.Append(removedId, new DecisionRemoved())).SequenceNumber;
            Removed = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(removedId);
            NeverCreated = await EventStore.ReadModels.GetInstanceForDecision<DecisionModel>(neverCreatedId);
            FirstMatchingAppend = await EventStore.EventLog.Append(neverCreatedId, new DecisionCreated("now exists"));
            AbsentStaleAppend = await EventStore.EventLog.AppendMany(
                [new EventForEventSourceId(Guid.NewGuid().ToString(), new DecisionChanged())],
                concurrencyScopes: NeverCreated.ToConcurrencyScopes());
        }
    }

    [Fact] void should_fold_the_created_instance() => Context.Created.Instance!.Name.ShouldEqual("first");
    [Fact] void should_reject_a_stale_decision_appended_to_another_source() => Context.StaleAppend.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_name_the_read_source_in_the_violation() => ((string)((dynamic)Context.StaleAppend).ConcurrencyViolation.EventSourceId.ToString()).ShouldEqual(Context.CreatedId.ToString());
    [Fact] void should_allow_a_fresh_decision_appended_to_another_source() => Context.FreshAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_removed_as_absent_with_a_watermark() => Context.Removed.Instance.ShouldBeNull();
    [Fact] void should_keep_the_removal_watermark() => Context.Removed.SequenceNumber.ShouldEqual(Context.RemovalSequenceNumber);
    [Fact] void should_report_never_created_as_absent() => Context.NeverCreated.Instance.ShouldBeNull();
    [Fact] void should_guard_never_created_as_before_first() => Context.NeverCreated.ToConcurrencyScopes().Values.Single().ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_append_the_first_matching_event() => Context.FirstMatchingAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_an_append_after_the_never_created_key_changes() => Context.AbsentStaleAppend.HasConcurrencyViolations.ShouldBeTrue();
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
