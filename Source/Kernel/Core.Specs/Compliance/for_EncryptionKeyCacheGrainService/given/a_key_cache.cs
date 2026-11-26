// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService.given;

public class a_key_cache : for_EnvelopeSigningKeys.given.a_signing_key_cache
{
    protected IEncryptionKeyStorage _storage;
    protected ProtectedValueAddress _address;
    private protected EnvelopeSigningKeys.SigningOperation _operation;
    private protected EnvelopeSigningKey _original;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage, IEvictEncryptionKeyCache>();
        _address = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject");
        _operation = _cache.Begin(_key, 8, _commitment);
        _original = _operation.Acquire();
    }

    void Destroy() => _operation.Dispose();
}
