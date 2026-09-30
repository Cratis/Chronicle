// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only.and_the_same_value_is_forwarded_to_the_outbox.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

/// <summary>
/// The pattern that broke with a constraint applying to every event sequence: a fact validated in the event log is
/// forwarded to the outbox, and the outbox keeps an index of its own that only removal events appended to the
/// outbox could release. A later claim of the same value by another event source then succeeds in the event log but
/// is refused in the outbox forever. Scoped to the event log, the outbox claims nothing, so every forward succeeds -
/// while the event log still refuses a duplicate.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_same_value_is_forwarded_to_the_outbox(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueNameInEventLog)];
        public override IEnumerable<Type> EventTypes => [typeof(NameClaimed)];

        public IAppendResult FirstInEventLog { get; private set; }
        public IAppendResult FirstInOutbox { get; private set; }
        public IAppendResult SecondInOutbox { get; private set; }
        public IAppendResult SecondInEventLog { get; private set; }

        public async Task Because()
        {
            var outbox = EventStore.GetEventSequence(EventSequenceId.Outbox);
            var @event = new NameClaimed("acme");
            var first = Guid.NewGuid().ToString();
            var second = Guid.NewGuid().ToString();

            FirstInEventLog = await EventStore.EventLog.Append(first, @event);
            FirstInOutbox = await outbox.Append(first, @event);
            SecondInOutbox = await outbox.Append(second, @event);
            SecondInEventLog = await EventStore.EventLog.Append(second, @event);
        }
    }

    [Fact] void should_accept_the_first_claim_in_the_event_log() => Context.FirstInEventLog.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_forwarding_the_first_claim_to_the_outbox() => Context.FirstInOutbox.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_same_value_for_another_event_source_in_the_outbox() => Context.SecondInOutbox.IsSuccess.ShouldBeTrue();
    [Fact] void should_still_refuse_the_same_value_for_another_event_source_in_the_event_log() => Context.SecondInEventLog.HasConstraintViolations.ShouldBeTrue();
}
