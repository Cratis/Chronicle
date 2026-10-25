// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys.given;

public class a_key_store : Specification
{
    protected const string Identifier = "subject";
    protected IEncryptionKeyStorage _keys;
    protected EncryptionKeyAtRevision _pair;

    void Establish()
    {
        _pair = new(new Encryption().GenerateKey(), 2);
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _keys.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pair);
    }

    protected Task<EncryptionKey?> Release(IEncryptionKeyStorage keys) =>
        ReleaseKeys.TryGetForRelease(keys, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
