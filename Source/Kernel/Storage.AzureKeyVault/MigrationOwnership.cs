// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Claims the vault for one migration at a time.
/// </summary>
/// <remarks>
/// <para>
/// Key Vault has no conditional write, so the claim is an append-only record: every acquirer appends a version saying it holds
/// the lock, lists every version, and owns the lock only when no other acquirer holds it without having released it. Two acquirers
/// that append together both see each other and both stand down, so an ownership is never shared. A lock is never stolen on age,
/// and the secret is never deleted: releasing appends a version.
/// </para>
/// </remarks>
/// <param name="vault">The <see cref="ISecretVault"/>.</param>
internal class MigrationOwnership(ISecretVault vault)
{
    /// <summary>
    /// The name of the ownership secret. It does not start with a prefix a key name can have.
    /// </summary>
    internal const string LockName = "chronicle-internal--migration-lock";

    const string HeldPrefix = "held:";
    const string ReleasedPrefix = "released:";

    /// <summary>
    /// Tries to take ownership.
    /// </summary>
    /// <param name="owner">The identity of this migration.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>True when this migration owns the vault; false when another does or may. When false, nothing is held.</returns>
    public async Task<bool> TryAcquire(string owner, CancellationToken cancellationToken)
    {
        await vault.Write(LockName, HeldPrefix + owner, cancellationToken);
        if (await OtherHolders(owner, cancellationToken))
        {
            await Release(owner, cancellationToken);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Releases ownership by appending a version.
    /// </summary>
    /// <param name="owner">The identity of this migration.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> for continuation.</returns>
    public async Task Release(string owner, CancellationToken cancellationToken) =>
        await vault.Write(LockName, ReleasedPrefix + owner, cancellationToken);

    async Task<bool> OtherHolders(string owner, CancellationToken cancellationToken)
    {
        // Every owner is unique to one migration, so a release cancels its hold whatever the order the versions are listed in.
        var held = new HashSet<string>(StringComparer.Ordinal);
        var released = new HashSet<string>(StringComparer.Ordinal);
        foreach (var version in await vault.ListVersions(LockName, cancellationToken))
        {
            var value = (await vault.Read(LockName, version.Version, cancellationToken))?.Value ?? string.Empty;
            if (value.StartsWith(HeldPrefix, StringComparison.Ordinal))
            {
                held.Add(value[HeldPrefix.Length..]);
            }
            else if (value.StartsWith(ReleasedPrefix, StringComparison.Ordinal))
            {
                released.Add(value[ReleasedPrefix.Length..]);
            }
            else
            {
                // A version nobody here wrote: the lock cannot be told free.
                held.Add(value);
            }
        }

        held.ExceptWith(released);
        held.Remove(owner);
        return held.Count > 0;
    }
}
