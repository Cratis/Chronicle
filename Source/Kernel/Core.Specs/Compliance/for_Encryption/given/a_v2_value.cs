// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption.given;

public class a_v2_value : a_key
{
    protected byte[] _encrypted;

    void Establish() => _encrypted = Encryption.EncryptEnvelope("value"u8.ToArray(), new EncryptionKeyAtRevision(_key, 7), 2);
}
