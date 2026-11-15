// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Compliance.MongoDB.for_InternalSecretForPurpose;

public class when_building_keys_for_distinct_stores : Specification
{
    static readonly string[] _stores = ["sales_eu", "sales-eu", "SALES-EU", "A_B", "a-b", "A-B", "orders", "orders ", "é", "e\u0301", "1:x", new('x', 2000), new string('x', 2000) + "y"];
    string[] _keys;
    string _repeated;

    void Because()
    {
        _keys = _stores.Select(store => EncryptionKeyStorage.BuildInternalSecretId(store, InternalSecretPurpose.MutationReceiptDigest)).ToArray();
        _repeated = EncryptionKeyStorage.BuildInternalSecretId(_stores[0], InternalSecretPurpose.MutationReceiptDigest);
    }

    [Fact] void should_preserve_every_exact_store_identity() => _keys.Distinct().Count().ShouldEqual(_stores.Length);
    [Fact] void should_be_deterministic() => _repeated.ShouldEqual(_keys[0]);
}
