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
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public bool Conflicting;
        public bool Irrelevant;
        public bool ValidateOnly;
        public Guid Key;

        public override IEnumerable<Type> EventTypes => [typeof(DecisionCreated), typeof(DecisionUnrelated)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(DecisionState)];

        async Task Because()
        {
            Key = Guid.NewGuid();
            var read = await EventStore.GetDecisionReads().GetDetached<DecisionState>(Key.ToString("D"));
            await EventStore.EventLog.Append(Key.ToString("D"), new DecisionUnrelated());
            Irrelevant = (await EventStore.EventLog.AppendMany([], guardedBy: [read])).IsSuccess;

            await EventStore.EventLog.Append(Key.ToString("D"), new DecisionCreated("created"));
            Conflicting = (await EventStore.EventLog.AppendMany(
                [new EventForEventSourceId(Key.ToString("D"), new DecisionCreated("second"))],
                guardedBy: [read])).ConcurrencyViolations.Any();
            ValidateOnly = (await EventStore.EventLog.AppendMany([], guardedBy: [read])).ConcurrencyViolations.Any();
        }
    }

    [Fact] void should_accept_an_unrelated_event() => Context.Irrelevant.ShouldBeTrue();
    [Fact] void should_refuse_the_absence_race() => Context.Conflicting.ShouldBeTrue();
    [Fact] void should_refuse_validate_only_when_the_model_changed() => Context.ValidateOnly.ShouldBeTrue();
}

[EventType]
public record DecisionCreated(string Name);

[EventType]
public record DecisionUnrelated();

[Passive]
[FromEvent<DecisionCreated>]
public record DecisionState(Guid Id, string Name);

#pragma warning restore SA1402
