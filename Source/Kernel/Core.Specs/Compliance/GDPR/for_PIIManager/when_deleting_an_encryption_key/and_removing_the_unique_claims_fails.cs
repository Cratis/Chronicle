// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Events.Constraints;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// A unique-index claim derived from the subject's personal data still confirms the value, so an erasure that could
/// not delete it has not finished and must say so rather than report success.
/// </summary>
public class and_removing_the_unique_claims_fails : given.a_pii_manager
{
    IUniqueConstraintsStorage _uniqueConstraints;
    Exception _error;

    void Establish()
    {
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _uniqueConstraints = Substitute.For<IUniqueConstraintsStorage>();
        _storage.GetEventStore(EventStore).GetNamespace(EventStoreNamespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequences().Returns([EventSequenceId.Log]);
        namespaceStorage.GetUniqueConstraintsStorage(EventSequenceId.Log).Returns(_uniqueConstraints);
        _uniqueConstraints.RemoveForSubject(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).ThrowsAsync(new StoreUnreachable());
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_try_to_remove_the_subjects_claims() => _uniqueConstraints.Received(1).RemoveForSubject(new Subject(Identifier.Value), Arg.Any<CancellationToken>());
    [Fact] void should_still_delete_the_key() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_incomplete_erasure_to_the_caller() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
}
