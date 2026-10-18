// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

public class and_there_are_two_revisions : Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given.a_migrated_namespace_database
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    ComplianceKey _expected = new([1], [2]);
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        _store = new EncryptionKeyStorage(_database);
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _expected, new EncryptionKeyRevision(7u));
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new ComplianceKey([3], [4]), EncryptionKeyRevision.Initial);
    }

    async Task Because() => _result = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    [Fact] void should_report_the_highest_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] void should_report_the_public_key_at_that_revision() => _result!.Key.Public.ShouldEqual(_expected.Public);
    [Fact] void should_report_the_private_key_at_that_revision() => _result!.Key.Private.ShouldEqual(_expected.Private);
}
