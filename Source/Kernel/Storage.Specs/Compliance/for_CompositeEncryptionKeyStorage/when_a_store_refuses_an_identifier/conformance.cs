// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

public abstract class conformance : given.two_key_stores
{
    given.CapturingLogger<CompositeEncryptionKeyStorage> _logger;
    Exception _read;
    Exception _latest;
    Exception _has;
    Exception _erasure;
    Exception _delete;
    Exception _record;
    Exception _allow;
    Exception _save;
    Exception _provision;

    void Establish()
    {
        var refusing = Substitute.For<IEncryptionKeyStorage>();
        refusing.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.HasFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.DeleteFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.RecordErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.AllowNewKeyFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.SaveFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        refusing.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>()).ThrowsAsync(new EncryptionKeyIdentifierUnderReview());
        _logger = new();
        _composite = Compose(_logger, refusing);
    }

    protected abstract CompositeEncryptionKeyStorage Compose(given.CapturingLogger<CompositeEncryptionKeyStorage> logger, IEncryptionKeyStorage refusing);

    async Task Because()
    {
        _read = await Catch.Exception(() => _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _latest = await Catch.Exception(() => _composite.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _has = await Catch.Exception(() => _composite.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _erasure = await Catch.Exception(() => _composite.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _delete = await Catch.Exception(() => _composite.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _record = await Catch.Exception(() => _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _allow = await Catch.Exception(() => _composite.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _save = await Catch.Exception(() => _composite.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("saved")));
        _provision = await Catch.Exception(() => _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("provisioned")));
    }

    [Fact] public void should_refuse_a_read() => _read.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_a_latest_read() => _latest.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_a_presence_check() => _has.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_an_erasure_lookup() => _erasure.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_a_delete() => _delete.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_recording_an_erasure() => _record.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_allowing_a_new_key() => _allow.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_a_save() => _save.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] public void should_refuse_provisioning_on_the_primary() => _provision.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();

    [Fact]
    public void should_not_name_the_identifier_in_any_exception() =>
        new[] { _read, _latest, _has, _erasure, _delete, _record, _allow, _save, _provision }.Any(_ => _.ToString().Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] public void should_not_name_the_identifier_in_any_log_line() => _logger.Lines.Any(_ => _.Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] public void should_log_the_refusal() => _logger.Lines.ShouldNotBeEmpty();
}
