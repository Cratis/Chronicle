// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;

namespace Cratis.Chronicle.Storage.InMemory.Compliance.for_EncryptionKeyStorageContract;

public class when_using_the_storage_provider : EncryptionKeyStorageContract
{
    protected override IEncryptionKeyStorage CreateStorage() => new EncryptionKeyStorage();
}
