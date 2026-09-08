// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// Pins the baseline release-cycle behavior against a real MongoDB event sequence collection: a covered event
/// followed by its declared removal event frees the event source for the next cycle.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_constraint_was_released(MongoDBFixture fixture) : given.a_unique_event_types_constraints_storage(fixture)
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        await Append(_checkedOutEventType, _borrower);
        await Append(_returnedEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);

    [Fact] void should_be_allowed() => _isAllowed.ShouldBeTrue();
    [Fact] void should_have_no_sequence_number_to_report() => _sequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
