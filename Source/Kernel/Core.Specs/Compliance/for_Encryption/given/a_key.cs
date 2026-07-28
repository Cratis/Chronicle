// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption.given;

public class a_key : Specification
{
    protected Encryption _encryption;
    protected EncryptionKey _key;

    void Establish()
    {
        _encryption = new();
        _key = _encryption.GenerateKey();
    }
}
