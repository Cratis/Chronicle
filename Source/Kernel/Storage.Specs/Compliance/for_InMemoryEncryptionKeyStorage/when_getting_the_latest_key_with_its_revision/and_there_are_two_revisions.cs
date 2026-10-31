// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

public class and_there_are_two_revisions : given.an_in_memory_key_store
{
    EncryptionKey _latest;
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        _latest = KeyNamed("latest");
        await Save(_latest, new EncryptionKeyRevision(7u));
        await Save(KeyNamed("earlier"), EncryptionKeyRevision.Initial);
    }

    async Task Because() => _result = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    [Fact] void should_report_the_highest_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] void should_report_the_key_at_that_revision() => _result!.Key.ShouldEqual(_latest);
}
