// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_two_sources_concurrently_claim_the_same_value : given.a_unique_constraints_storage
{
    EventSourceId _firstOwner;
    EventSourceId _secondOwner;
    Exception?[] _errors;

    void Establish()
    {
        _firstOwner = EventSourceId.New();
        _secondOwner = EventSourceId.New();
    }

    async Task Because()
    {
        using var barrier = new System.Threading.Barrier(2);
        _errors = await Task.WhenAll(
            Task.Run(() => Claim(_firstOwner, 42UL)),
            Task.Run(() => Claim(_secondOwner, 43UL)));

        Task<Exception?> Claim(EventSourceId owner, EventSequenceNumber sequenceNumber)
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            return Catch.Exception(() => _storage.Save(owner, _definition, sequenceNumber, "shared"));
        }
    }

    [Fact] void should_accept_exactly_one_claim() => _errors.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_report_the_losing_claim_as_a_constraint_collision() => _errors.Single(_ => _ is not null).ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] async Task should_allow_the_winning_owner() => (await _storage.IsAllowed(_errors[0] is null ? _firstOwner : _secondOwner, _definition, "shared")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_refuse_the_losing_owner() => (await _storage.IsAllowed(_errors[0] is null ? _secondOwner : _firstOwner, _definition, "shared")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_record_the_winning_claim_position() => (await _storage.IsAllowed(EventSourceId.New(), _definition, "shared")).SequenceNumber.ShouldEqual((EventSequenceNumber)(_errors[0] is null ? 42UL : 43UL));
}
