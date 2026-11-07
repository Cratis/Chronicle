// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKeys;

public class when_a_subject_key_is_erased : given.a_key_store
{
    static readonly EncryptionKeyIdentifier _subject = new("subject");

    byte[] _before;
    byte[] _afterInNewProcess;
    bool _subjectKeyExists;

    async Task Establish()
    {
        await _keyStore.SaveFor("event-store", EventStoreNamespaceName.Default, _subject, new(Encoding.UTF8.GetBytes("public"), Encoding.UTF8.GetBytes("private")));
        _before = (await KeysOver(_keyStore).TryGetFor("event-store"))!.Compute(_canonical);
    }

    async Task Because()
    {
        await _keyStore.DeleteFor("event-store", EventStoreNamespaceName.Default, _subject);
        await _keyStore.RecordErasureFor("event-store", EventStoreNamespaceName.Default, _subject);
        _subjectKeyExists = await _keyStore.HasFor("event-store", EventStoreNamespaceName.Default, _subject);

        // A fresh provider has no cache, so it reads the secret from the key store again.
        _afterInNewProcess = (await KeysOver(_keyStore).TryGetFor("event-store"))!.Compute(_canonical);
    }

    [Fact] void should_have_erased_the_subject_key() => _subjectKeyExists.ShouldBeFalse();
    [Fact] void should_keep_the_digest_key() => _afterInNewProcess.SequenceEqual(_before).ShouldBeTrue();
}
