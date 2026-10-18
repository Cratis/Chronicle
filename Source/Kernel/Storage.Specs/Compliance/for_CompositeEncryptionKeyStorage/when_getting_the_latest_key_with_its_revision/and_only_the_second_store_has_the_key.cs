// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

public class and_only_the_second_store_has_the_key : Specification
{
    InMemoryEncryptionKeyStorage _first;
    InMemoryEncryptionKeyStorage _second;
    CompositeEncryptionKeyStorage _composite;
    EncryptionKey _key = new([1], [2]);
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        _first = new();
        _second = new();
        _composite = new(_first, _second);
        await _second.SaveFor("store", "tenant", "subject", _key, new EncryptionKeyRevision(7u));
    }

    async Task Because() => _result = await _composite.TryGetLatestFor("store", "tenant", "subject");

    [Fact] void should_report_the_found_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] async Task should_heal_at_the_exact_revision() => (await _first.TryGetLatestFor("store", "tenant", "subject"))!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
}
