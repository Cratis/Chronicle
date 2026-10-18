// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

public class and_resolving_identities_fails_after_the_insert : given.a_storage_for_appended_generation
{
    Exception _error;
    EventEntry _stored;
    void Establish() => _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).ThrowsAsync(new given.AcknowledgmentLost());
    async Task Because()
    {
        _error = await Catch.Exception(() => Append(EventSequenceNumber.First));
        _stored = Stored(EventSequenceNumber.First);
    }
    [Fact] void should_throw_the_original_error() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_not_claim_the_write_was_aborted() => (_error is EventsNotCommitted).ShouldBeFalse();
    [Fact] void should_have_stored_the_event() => _stored.SequenceNumber.ShouldEqual(0UL);
}
