// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.InMemory.Compliance.for_EncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

public class and_subject_keys_share_its_name : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EncryptionKeyIdentifier _alias = InternalSecretPurpose.MutationReceiptDigest.Value;
    static readonly EncryptionKey _secret = new([1], [2]);

    IEncryptionKeyStorage _store;
    EncryptionKey _afterSubjectLifecycle;
    bool _subjectKeyExists;

    async Task Establish()
    {
        _store = new EncryptionKeyStorage();
        await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, _secret);
    }

    async Task Because()
    {
        await _store.GetOrAddFor(_eventStore, EventStoreNamespaceName.Default, _alias, new EncryptionKey([7], [7]));
        await _store.RecordErasureFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.DeleteFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.AllowNewKeyFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _subjectKeyExists = await _store.HasFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _afterSubjectLifecycle = await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, new EncryptionKey([3], [4]));
    }

    [Fact] void should_have_erased_the_subject_key() => _subjectKeyExists.ShouldBeFalse();
    [Fact] void should_keep_the_internal_secret() => _afterSubjectLifecycle.Private.ShouldEqual(_secret.Private);
}
