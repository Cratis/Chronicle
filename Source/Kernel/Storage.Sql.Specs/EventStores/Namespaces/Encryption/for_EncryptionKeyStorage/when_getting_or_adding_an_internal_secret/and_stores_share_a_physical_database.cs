// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

public class and_stores_share_a_physical_database : a_migrated_namespace_database
{
    static readonly string[] _stores = ["sales_eu", "sales-eu", "SALES-EU", "A_B", "a-b", "A-B", "orders", "orders ", "é", "e\u0301"];
    List<byte> _stored;
    List<byte> _reloaded;

    async Task Because()
    {
        var storage = new EncryptionKeyStorage(_database);
        _stored = [];
        _reloaded = [];
        for (var index = 0; index < _stores.Length; index++)
        {
            var secret = await storage.GetOrAddInternalSecretFor(_stores[index], InternalSecretPurpose.MutationReceiptDigest, new ComplianceKey([0], [(byte)index]));
            _stored.Add(secret.Private[0]);
        }

        var anotherSilo = new EncryptionKeyStorage(_database);
        foreach (var store in _stores)
        {
            var secret = await anotherSilo.GetOrAddInternalSecretFor(store, InternalSecretPurpose.MutationReceiptDigest, new ComplianceKey([0], [255]));
            _reloaded.Add(secret.Private[0]);
        }
    }

    [Fact] void should_keep_every_stores_own_secret() => _stored.ShouldEqual(Enumerable.Range(0, _stores.Length).Select(index => (byte)index));
    [Fact] void should_return_the_same_secret_on_another_silo() => _reloaded.ShouldEqual(_stored);
}
