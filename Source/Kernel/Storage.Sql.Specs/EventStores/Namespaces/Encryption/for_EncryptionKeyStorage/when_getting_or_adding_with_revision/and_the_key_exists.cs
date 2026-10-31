// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_getting_or_adding_with_revision;

public class and_the_key_exists : Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given.a_migrated_namespace_database
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    ComplianceKey _expected = new([1], [2]);
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        _store = new EncryptionKeyStorage(_database);
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _expected, new EncryptionKeyRevision(7u));
    }

    async Task Because() => _result = await _store.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new ComplianceKey([3], [4]));

    [Fact] void should_report_the_existing_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] void should_return_the_existing_key() => _result!.Key.Public.ShouldEqual(_expected.Public);
    [Fact] async Task should_not_mint_another_revision() => (await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKeyRevision(8u))).ShouldBeFalse();
}
