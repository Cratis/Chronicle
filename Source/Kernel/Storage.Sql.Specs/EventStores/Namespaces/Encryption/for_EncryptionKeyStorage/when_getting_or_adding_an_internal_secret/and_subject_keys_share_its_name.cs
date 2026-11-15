// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

public class and_subject_keys_share_its_name : Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given.a_migrated_namespace_database
{
    static readonly EncryptionKeyIdentifier _alias = InternalSecretPurpose.MutationReceiptDigest.Value;
    static readonly ComplianceKey _secret = new([1], [2]);

    IEncryptionKeyStorage _store;
    ComplianceKey _afterSubjectLifecycle;
    ComplianceKey _secondCandidateResult;
    bool _subjectKeyExists;

    async Task Establish()
    {
        _store = new EncryptionKeyStorage(_database);
        await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, _secret);
    }

    async Task Because()
    {
        await _store.GetOrAddFor(_eventStore, EventStoreNamespaceName.Default, _alias, new ComplianceKey([7], [7]));
        await _store.RecordErasureFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.DeleteFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.AllowNewKeyFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _subjectKeyExists = await _store.HasFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _afterSubjectLifecycle = await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, new ComplianceKey([3], [4]));
        _secondCandidateResult = await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, new ComplianceKey([5], [6]));
    }

    [Fact] void should_have_erased_the_subject_key() => _subjectKeyExists.ShouldBeFalse();
    [Fact] void should_keep_the_internal_secret() => _afterSubjectLifecycle.Private.ShouldEqual(_secret.Private);
    [Fact] void should_not_replace_the_stored_secret_with_a_later_candidate() => _secondCandidateResult.Private.ShouldEqual(_secret.Private);
}
