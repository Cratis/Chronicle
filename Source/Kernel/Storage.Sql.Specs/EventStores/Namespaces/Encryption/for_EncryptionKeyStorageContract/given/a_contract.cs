// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorageContract.given;

public abstract class a_contract(KeyStoreHarness harness) : EncryptionKeyStorageContract
{
    KeyStoreHarness _harness;
    FenceInterleaving _interleaving;

    protected override IEncryptionKeyStorage CreateStorage()
    {
        _harness = harness;
        _harness.Initialize().GetAwaiter().GetResult();
        _interleaving = new();
        _harness.Interceptor = _interleaving;
        return _harness.Storage;
    }

    protected override Task DuringPublication(Func<Task> write, Func<Task> competingOperation) => _interleaving.DuringPublication(write, competingOperation);
    protected override Task DuringErasure(Func<Task> erase, Func<Task> competingOperation) => _interleaving.DuringErasure(erase, competingOperation);
    protected override Task LoseWriteResponse(Func<Task> write) => _interleaving.LoseResponse(write);

    async Task Destroy() => await _harness.DisposeAsync();
}
