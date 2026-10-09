// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_publishing;

public class and_retrying_with_a_different_slot : given.a_publication_storage
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _result;

    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    async Task Because() => _result = await _storage.AppendPublication(_publication, _event with { SequenceNumber = 19 });

    [Fact] void should_return_the_original_slot() => _result.AsT0.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_append_again() => _storage.Events.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_subject() => _storage.Events.Single().Context.Subject.ShouldEqual(_event.Subject);
    [Fact] void should_preserve_the_tenant() => _storage.Events.Single().Context.Namespace.ShouldEqual((Concepts.EventStoreNamespaceName)"tenant");
}
