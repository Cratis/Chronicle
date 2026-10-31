// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_append_metadata_collections_were_changed_by_the_caller : given.a_metadata_storage
{
    IdentityId _original;

    async Task Establish()
    {
        _original = _entry.CausedByChain.Single();
        IdentityId[] chain = [_original];
        _entry = _entry with { CausedByChain = chain };
        await _storage.AppendMany([_entry]);
        chain[0] = IdentityId.New();
        _entry.Causation.Single().Properties["value"] = "changed";
    }

    async Task Because() => await Read();

    [Fact] void should_keep_the_stored_identity_chain() => _result.CausedByChain.ShouldContainOnly(_original);
    [Fact] void should_keep_the_stored_causation_properties() => _result.Causation.Single().Properties["value"].ShouldEqual("original");
}
