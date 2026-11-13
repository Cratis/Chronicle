// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;
using Microsoft.EntityFrameworkCore;
using Fence = Cratis.Chronicle.Storage.Compliance.EncryptionKeyErasure;
using Key = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

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

    protected override async Task SeedRequests(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int count)
    {
        await using var context = _harness.Context();
        var stored = EncryptionKeyIdentifierColumn.Value(context, identifier);
        var row = await context.EncryptionKeyErasures.AsNoTracking().SingleAsync(_ => _.Identifier == stored);
        var fence = EncryptionKeyFenceWrites.Read(identifier, row);
        var present = await context.EncryptionKeys.AsNoTracking().Where(_ => _.Identifier == stored).ToListAsync();
        var material = present.Select(_ => ((EncryptionKeyRevision)_.Revision, new Key(_.PublicKey, _.PrivateKey))).ToArray();
        var requests = fence.Requests!.ToList();
        for (var index = 1; index <= count; index++)
        {
            fence = Fence.Covering(fence, material);
            requests.Add(new($"request-{index}", fence.ErasedThrough, fence.Lifecycle, fence.ErasedKeyFingerprints));
        }

        var next = (fence with { Requests = requests }).WithStateSeal();
        (await EncryptionKeyFenceWrites.CompareExchange(context, identifier, row, next)).ShouldEqual(1);
    }

    protected override Task DuringPublication(Func<Task> write, Func<Task> competingOperation) => _interleaving.DuringPublication(write, competingOperation);
    protected override Task DuringErasure(Func<Task> erase, Func<Task> competingOperation) => _interleaving.DuringErasure(erase, competingOperation);
    protected override Task LoseWriteResponse(Func<Task> write) => _interleaving.LoseResponse(write);

    async Task Destroy() => await _harness.DisposeAsync();
}
