// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

public class and_the_subject_holds_unique_claims : given.a_pii_manager
{
    static readonly EventSequenceId _otherSequence = "other-sequence";
    IUniqueConstraintsStorage _log;
    IUniqueConstraintsStorage _other;
    Exception _error;

    void Establish()
    {
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _log = Substitute.For<IUniqueConstraintsStorage>();
        _other = Substitute.For<IUniqueConstraintsStorage>();
        _storage.GetEventStore(EventStore).GetNamespace(EventStoreNamespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequences().Returns([EventSequenceId.Log, _otherSequence]);
        namespaceStorage.GetUniqueConstraintsStorage(EventSequenceId.Log).Returns(_log);
        namespaceStorage.GetUniqueConstraintsStorage(_otherSequence).Returns(_other);
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_complete() => _error.ShouldBeNull();
    [Fact] void should_remove_the_subjects_claims_from_the_log() => _log.Received(1).RemoveForSubject(new Subject(Identifier.Value), Arg.Any<CancellationToken>());
    [Fact] void should_remove_the_subjects_claims_from_every_other_event_sequence() => _other.Received(1).RemoveForSubject(new Subject(Identifier.Value), Arg.Any<CancellationToken>());
}
