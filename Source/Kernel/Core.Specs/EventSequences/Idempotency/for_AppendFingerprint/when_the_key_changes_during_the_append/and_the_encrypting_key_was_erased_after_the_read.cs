// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_the_key_changes_during_the_append;

public class and_the_encrypting_key_was_erased_after_the_read : given.an_append_intent
{
    async Task Establish()
    {
        Protect();
        var store = new InMemoryEncryptionKeyStorage();
        await store.SaveFor("store", "ns", "subject", _key);
        await store.RecordErasureFor("store", "ns", "subject");
        await store.DeleteFor("store", "ns", "subject");
        await store.AllowNewKeyFor("store", "ns", "subject");
        await store.GetOrAddWithRevisionFor("store", "ns", "subject", new([7, 8, 9], [10, 11, 12]));
    }
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.Pii, _key));
    [Fact] void should_reference_the_reported_key() => _result.ProtectedParts[0].KeyReference.ShouldEqual(_key.Fingerprint);
    [Fact] void should_preserve_the_reported_revision() => _result.ProtectedParts[0].Revision.Value.ShouldEqual(1u);
    [Fact] void should_preserve_the_intent_despite_the_rotation() => _result.Format().ShouldEqual(_original.Format());
}
