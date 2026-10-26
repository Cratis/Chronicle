// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey.given;

public class secret_material : Specification
{
    protected UniqueConstraintStoreKey _material;
    protected UniqueConstraintEncodingKey _containing;

    void Establish()
    {
        _material = new UniqueConstraintStoreKey(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
        _containing = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("00112233445566778899aabbccddeeff"), _material);
    }
}
