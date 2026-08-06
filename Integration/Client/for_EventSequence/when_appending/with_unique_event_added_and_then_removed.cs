// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_unique_event_added_and_then_removed.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

/// <summary>
/// The whole path for the per-event-source form of uniqueness: the constraint is declared on the client, registered
/// over the wire, and enforced by the kernel against the real store. Every layer in between used to drop the removal
/// event - the client discarded it before the contract, and the kernel had nowhere to put it - so a constraint
/// declared as releasing behaved as "at most one, forever" with nothing anywhere reporting a problem.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class with_unique_event_added_and_then_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueEventReleasedByRemovalConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        UserOnboardingStarted _event;
        UserRemoved _removedEvent;

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult RemovedResult { get; private set; }
        public IAppendResult NextCycleResult { get; private set; }
        public IAppendResult SameCycleResult { get; private set; }

        public void Establish()
        {
            _event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            _removedEvent = new UserRemoved();
        }

        public async Task Because()
        {
            var eventSourceId = Guid.NewGuid().ToString();
            FirstResult = await EventStore.EventLog.Append(eventSourceId, _event);
            RemovedResult = await EventStore.EventLog.Append(eventSourceId, _removedEvent);
            NextCycleResult = await EventStore.EventLog.Append(eventSourceId, _event);
            SameCycleResult = await EventStore.EventLog.Append(eventSourceId, _event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_remove_attempt() => Context.RemovedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_the_next_cycle() => Context.NextCycleResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_within_the_same_cycle() => Context.SameCycleResult.IsSuccess.ShouldBeFalse();
}
