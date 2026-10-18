// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using LaggingReadKeyStorage = Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_provisioning_a_key_concurrently.given.a_key_store_with_read_after_write_lag.LaggingReadKeyStorage;

namespace Cratis.Chronicle.ProtectedValues.for_ManagedEncryptionKeyProvisioner.when_ensuring_a_key_with_its_revision;

public class and_two_callers_race : Specification
{
    LaggingReadKeyStorage _storage;
    ManagedEncryptionKeyProvisioner _first;
    ManagedEncryptionKeyProvisioner _second;
    EncryptionKeyAtRevision[] _results;

    void Establish()
    {
        _storage = new();
        var encryption = Substitute.For<IEncryption>();
        encryption.GenerateKey().Returns(new EncryptionKey([1], [2]), new EncryptionKey([3], [4]));
        _first = new(_storage, encryption);
        _second = new(_storage, encryption);
    }

    async Task Because() => _results = await Task.WhenAll(_first.EnsureKeyWithRevisionFor("store", "tenant", "subject"), _second.EnsureKeyWithRevisionFor("store", "tenant", "subject"));

    [Fact] void should_converge_on_the_same_pair() => _results[1].ShouldEqual(_results[0]);
    [Fact] void should_report_the_initial_revision() => _results[0].Revision.ShouldEqual(EncryptionKeyRevision.Initial);
    [Fact] async Task should_persist_only_one_revision() => (await _storage.RevisionCountOnPrimary("store", "tenant", "subject")).ShouldEqual(1);
}
