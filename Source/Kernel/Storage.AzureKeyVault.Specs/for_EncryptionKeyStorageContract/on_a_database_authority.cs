// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class on_a_database_authority : EncryptionKeyStorageContract
{
    given.VersionedSecretClient _client;
    given.FenceAuthority _authority;

    protected override IEncryptionKeyStorage CreateStorage()
    {
        _client = new();
        _authority = new();
        return new AzureKeyVaultEncryptionKeyStorage(_client, null, _authority, _ => Task.CompletedTask);
    }

    protected override async Task DuringPublication(Func<Task> write, Func<Task> competingOperation)
    {
        _client.AfterStage = competingOperation;
        await write();
    }

    protected override async Task DuringErasure(Func<Task> erase, Func<Task> competingOperation)
    {
        _authority.BeforeErasure = competingOperation;
        await erase();
    }

    protected override async Task LoseWriteResponse(Func<Task> write)
    {
        _authority.LoseResponse = true;
        await write();
    }
}
