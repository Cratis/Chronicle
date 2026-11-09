// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

/// <summary>
/// Fault injection only: every normal operation still reaches the live Vault.
/// The interruption boundary is AFTER Vault has acknowledged the destination write.
/// </summary>
/// <param name="inner">The real Vault store to forward operations to.</param>
internal sealed class FaultingSecretStore(IVaultSecretStore inner) : IVaultSecretStore
{
    internal TaskCompletionSource CopyWritten { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool FailReadback { get; init; }
    internal bool InterruptAfterCopy { get; init; }
    internal bool FailLockRelease { get; init; }
    internal CancellationToken Interruption { get; init; }
    internal Func<Task<bool>>? ObserveDeletion { get; init; }
    internal bool AllDeletionsFollowedVerification { get; private set; } = true;
    internal int Deletions { get; private set; }

    public Task<bool> IsKv2() => inner.IsKv2();
    public Task<IReadOnlyList<string>> List(string path) => inner.List(path);
    public Task<bool> IsTombstone(string path) => inner.IsTombstone(path);

    public Task<VaultSecret?> Read(string path) => FailReadback && CopyWritten.Task.IsCompleted && path == "k1/store/default/customer-42/1"
        ? Task.FromResult<VaultSecret?>(null)
        : inner.Read(path);

    public async Task<int?> WriteIfAbsent(string path, IReadOnlyDictionary<string, string> data)
    {
        var version = await inner.WriteIfAbsent(path, data);
        if (path == "k1/store/default/customer-42/1" && version is not null)
        {
            CopyWritten.TrySetResult();
            if (InterruptAfterCopy)
            {
                // A deliberately stalled test double exposes the acknowledged-write interruption boundary.
                await Task.Delay(Timeout.InfiniteTimeSpan, Interruption);
            }
        }

        return version;
    }

    public Task Remove(string path)
    {
        if (FailLockRelease && path == VaultKeyStoreMigrator.MigrationLockPath)
        {
            throw new IOException("Injected lock-release interruption.");
        }

        return inner.Remove(path);
    }

    public async Task<bool> RemoveVersionIfUnchanged(string path, VaultSecret expected, Func<Task<bool>> canRemove)
    {
        Deletions++;
        if (ObserveDeletion is not null)
        {
            AllDeletionsFollowedVerification &= await ObserveDeletion();
        }

        return await inner.RemoveVersionIfUnchanged(path, expected, canRemove);
    }
}
