// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only.and_the_constraint_applies_to_every_event_sequence.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

/// <summary>
/// The contrast that makes the event-log-only specification meaningful: a constraint that declares no event
/// sequences keeps the default behavior and is enforced in the outbox too.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_constraint_applies_to_every_event_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueNameEverywhere)];
        public override IEnumerable<Type> EventTypes => [typeof(NameClaimedEverywhere)];

        public IAppendResult FirstInOutbox { get; private set; }
        public IAppendResult SecondInOutbox { get; private set; }

        public async Task Because()
        {
            var outbox = EventStore.GetEventSequence(EventSequenceId.Outbox);
            var @event = new NameClaimedEverywhere("globex");

            FirstInOutbox = await outbox.Append(Guid.NewGuid().ToString(), @event);
            SecondInOutbox = await outbox.Append(Guid.NewGuid().ToString(), @event);
        }
    }

    [Fact] void should_accept_the_first_claim_in_the_outbox() => Context.FirstInOutbox.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_the_same_value_for_another_event_source_in_the_outbox() => Context.SecondInOutbox.HasConstraintViolations.ShouldBeTrue();
}
