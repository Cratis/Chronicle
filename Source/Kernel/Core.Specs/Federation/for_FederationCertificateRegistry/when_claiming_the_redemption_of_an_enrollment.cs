// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Federation.for_FederationCertificateRegistry;

public class when_claiming_the_redemption_of_an_enrollment : Specification
{
    IConstraintsStorage _constraints;
    IEventSequence _eventLog;
    FederationCertificateRegistry _registry;
    IConstraintDefinition _saved;
    bool _first;
    bool _second;
    List<object> _events;

    void Establish()
    {
        _events = [];
        _constraints = Substitute.For<IConstraintsStorage>();
        _constraints.SaveDefinition(Arg.Do<IConstraintDefinition>(_ => _saved = _)).Returns(Task.CompletedTask);

        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Constraints.Returns(_constraints);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(EventStoreName.System).Returns(eventStore);

        _eventLog = Substitute.For<IEventSequence>();
        var claimed = new HashSet<string>();
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<Causation>?>(),
            Arg.Any<Identity?>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>())
            .Returns(call =>
            {
                _events.Add(call.ArgAt<object>(1));

                // What the event log does with the unique event type constraint: the second event of the type for the same source is refused.
                return Task.FromResult(claimed.Add(call.ArgAt<EventSourceId>(0).Value)
                    ? AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First)
                    : AppendResult.Failed(CorrelationId.New(), [new ConstraintViolation(typeof(FederationEnrollmentRedeemed).GetEventType().Id, EventSequenceNumber.Unavailable, ConstraintType.Unique, "FederationEnrollmentRedeemedOnce", "already redeemed", new ConstraintViolationDetails())]));
            });
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);

        _registry = new(storage, grainFactory, Substitute.For<IEventSerializer>());
    }

    async Task Because()
    {
        _first = await _registry.ClaimRedemption("enrollment-1", "serial-1");
        _second = await _registry.ClaimRedemption("enrollment-1", "serial-2");
    }

    [Fact] void should_let_the_first_claim_through() => _first.ShouldBeTrue();
    [Fact] void should_refuse_the_second_claim() => _second.ShouldBeFalse();
    [Fact] void should_append_the_redemption_to_the_enrollment() => _events.OfType<FederationEnrollmentRedeemed>().Count().ShouldEqual(2);
    [Fact] void should_have_registered_a_unique_event_type_constraint() => _saved.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();

    [Fact]
    void should_have_registered_it_for_the_redemption_event() =>
        ((UniqueEventTypeConstraintDefinition)_saved).EventTypeIds.ShouldContainOnly(typeof(FederationEnrollmentRedeemed).GetEventType().Id);
}
