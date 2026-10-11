// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_a_constraint_for_the_event_log_only_is_widened.and_a_value_was_forwarded_to_the_outbox_while_it_was_narrow.context;
using UniqueConstraintDefinition = Cratis.Chronicle.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_a_constraint_for_the_event_log_only_is_widened;

/// <summary>
/// While the constraint applies to the event log alone, the outbox neither validates nor indexes it, so a value
/// forwarded to the outbox claims nothing there. Widening the constraint to every event sequence must rebuild the
/// outbox's index from the events already in it - otherwise the outbox would accept the same value from another
/// event source, as if it had never been claimed. The rebuild is started when the widened definition is registered,
/// not when the outbox next appends, so it has completed before that append is validated - and it does not depend on
/// the outbox's grain being active at the time. The outbox, active here, refreshes its constraints before the rebuild
/// starts, so the specification waits for nothing but the rebuild.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_a_value_was_forwarded_to_the_outbox_while_it_was_narrow(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueReservedName)];
        public override IEnumerable<Type> EventTypes => [typeof(NameReserved), typeof(PublicNameReserved)];

        public IAppendResult FirstInOutbox { get; private set; }
        public IAppendResult SecondInOutbox { get; private set; }

        public async Task Because()
        {
            var outbox = EventStore.GetEventSequence(EventSequenceId.Outbox);
            var @event = new NameReserved("initech");
            var forwarded = new PublicNameReserved(@event.Name);

            await EventStore.EventLog.Append(Guid.NewGuid().ToString(), @event);
            FirstInOutbox = await outbox.Append(Guid.NewGuid().ToString(), forwarded);

            // Registering refreshes the outbox's constraints and starts its reindex before it returns, so all that is
            // left to wait for is the reindex completing.
            await RegisterForEveryEventSequence();
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30), Cratis.Chronicle.Jobs.JobStatus.CompletedSuccessfully);

            SecondInOutbox = await outbox.Append(Guid.NewGuid().ToString(), forwarded);
        }

        Task RegisterForEveryEventSequence()
        {
            var definition = (UniqueConstraintDefinition)EventStore.Constraints.GetFor(nameof(UniqueReservedName));
            var servicesAccessor = (IChronicleServicesAccessor)EventStore.Connection;
            return servicesAccessor.Services.Constraints.Register(new RegisterConstraintsRequest
            {
                EventStore = EventStore.Name,
                Constraints =
                [
                    new Constraint
                    {
                        Name = definition.Name,
                        Type = ConstraintType.Unique,
                        Definition = new(new Contracts.Events.Constraints.UniqueConstraintDefinition
                        {
                            EventDefinitions = definition.EventsWithProperties.Select(_ => new UniqueConstraintEventDefinition
                            {
                                EventTypeId = _.EventTypeId,
                                Properties = [.. _.Properties]
                            }).ToList()
                        })
                    }
                ]
            });
        }
    }

    [Fact] void should_accept_forwarding_the_value_while_the_constraint_is_narrow() => Context.FirstInOutbox.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_the_same_value_for_another_event_source_in_the_outbox_once_widened() => Context.SecondInOutbox.HasConstraintViolations.ShouldBeTrue();
}
