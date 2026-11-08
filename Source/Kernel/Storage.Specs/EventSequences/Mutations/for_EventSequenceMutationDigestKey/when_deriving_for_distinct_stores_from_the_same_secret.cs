// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKey;

public class when_deriving_for_distinct_stores_from_the_same_secret : Specification
{
    static readonly string[] _stores = ["sales_eu", "sales-eu", "SALES-EU", "A_B", "a-b", "A-B", "orders", "orders ", "é", "e\u0301", new('x', 2000), new string('x', 2000) + "y"];
    static readonly byte[] _secret = new byte[EventSequenceMutationDigestKey.MinimumSecretLength];
    static readonly byte[] _canonical = Encoding.UTF8.GetBytes("canonical");
    string[] _digests;
    string _repeated;

    void Because()
    {
        _digests = _stores.Select(store => Convert.ToHexString(EventSequenceMutationDigestKey.DeriveFrom(store, _secret).Compute(_canonical))).ToArray();
        _repeated = Convert.ToHexString(EventSequenceMutationDigestKey.DeriveFrom(_stores[0], _secret).Compute(_canonical));
    }

    [Fact] void should_bind_every_digest_to_the_exact_store() => _digests.Distinct().Count().ShouldEqual(_stores.Length);
    [Fact] void should_be_stable_for_the_same_store_and_secret() => _repeated.ShouldEqual(_digests[0]);
}
