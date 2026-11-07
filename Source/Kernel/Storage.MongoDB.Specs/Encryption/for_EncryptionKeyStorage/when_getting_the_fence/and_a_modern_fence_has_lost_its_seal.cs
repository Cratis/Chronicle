// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_the_fence;

public class and_a_modern_fence_has_lost_its_seal : racing_encryption_key_collections
{
    Exception _result;

    void Establish()
    {
        _sealFence = false;
        _fence = new("identifier", 7u, [], false, 1);
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_refuse_to_attest_the_downgraded_fence() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
