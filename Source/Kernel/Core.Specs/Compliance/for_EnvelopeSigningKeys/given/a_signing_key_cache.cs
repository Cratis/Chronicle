// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys.given;

public class a_signing_key_cache : Specification
{
    private protected EnvelopeSigningKeys _cache;
    protected EncryptionKey _key;
    protected byte[] _commitment;

    void Establish()
    {
        _cache = new();
        _key = new Encryption(null, new()).GenerateKey();
        _commitment = EnvelopeSigning.Commit(new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject"));
    }
}
