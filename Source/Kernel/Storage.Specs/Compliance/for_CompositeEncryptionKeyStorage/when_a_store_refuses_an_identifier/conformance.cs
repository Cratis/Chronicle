// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

public abstract class conformance : given.two_key_stores
{
    given.CapturingLogger<CompositeEncryptionKeyStorage> _logger;
    Exception _refusal;
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
        _refusal = Refusal();
        var refusing = Substitute.For<IEncryptionKeyStorage>();
        refusing.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
        refusing.GetErasureForRelease(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(_refusal);
        refusing.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(_refusal);
        refusing.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(_refusal);
        refusing.HasFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(_refusal);
        refusing.GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(_refusal);
        refusing.DeleteFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(_refusal);
        refusing.RecordErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(_refusal);
        refusing.AllowNewKeyFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(_refusal);
        refusing.SaveFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(_refusal);
        refusing.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>()).ThrowsAsync(_refusal);
        _logger = new();
        _composite = Compose(_logger, refusing);
    }

    protected abstract CompositeEncryptionKeyStorage Compose(given.CapturingLogger<CompositeEncryptionKeyStorage> logger, IEncryptionKeyStorage refusing);

    protected virtual Exception Refusal() => new EncryptionKeyIdentifierUnderReview();

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

    [Fact] public void should_refuse_a_read() => _read.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_a_latest_read() => _latest.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_a_presence_check() => _has.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_an_erasure_lookup() => _erasure.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_a_delete() => _delete.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_recording_an_erasure() => _record.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_allowing_a_new_key() => _allow.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_a_save() => _save.ShouldBeSame(_refusal);
    [Fact] public void should_refuse_provisioning_on_the_primary() => _provision.ShouldBeSame(_refusal);

    [Fact]
    public void should_not_name_the_identifier_in_any_exception() =>
        new[] { _read, _latest, _has, _erasure, _delete, _record, _allow, _save, _provision }.Any(_ => _.ToString().Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] public void should_not_name_the_identifier_in_any_log_line_about_the_refusal() => _logger.Lines.Any(_ => _.Contains("under review", StringComparison.OrdinalIgnoreCase) && _.Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] public void should_not_name_the_identifier_in_a_log_line_when_every_member_answers() => _logger.Lines.Any(_ => !_.Contains("StoreUnreachable", StringComparison.Ordinal) && _.Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] public void should_log_the_refusal() => _logger.Lines.ShouldNotBeEmpty();
}
