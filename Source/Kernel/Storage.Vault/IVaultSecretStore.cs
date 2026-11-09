// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Defines the operations on a KV v2 mount that inspecting and migrating the key layout need.
/// </summary>
internal interface IVaultSecretStore
{
    /// <summary>
    /// Verifies that the configured mount is KV v2 before using versioned operations.
    /// </summary>
    /// <returns>Whether the mount is proven to be KV v2.</returns>
    Task<bool> IsKv2();

    /// <summary>
    /// Removes only the observed version, after checking that it is still current.
    /// </summary>
    /// <param name="path">The path of the secret.</param>
    /// <param name="expected">The observed secret.</param>
    /// <param name="canRemove">A final check of the other secrets on which deletion depends.</param>
    /// <returns>Whether that version was removed.</returns>
    Task<bool> RemoveVersionIfUnchanged(string path, VaultSecret expected, Func<Task<bool>> canRemove);

    /// <summary>
    /// Lists the entries directly under a path. Directories end in <c language="csharp">/</c>.
    /// </summary>
    /// <param name="path">The path, or an empty string for the root of the mount.</param>
    /// <returns>The entries, or none when the path does not exist.</returns>
    Task<IReadOnlyList<string>> List(string path);

    /// <summary>
    /// Checks metadata for a secret whose versions are all deleted or destroyed.
    /// </summary>
    /// <param name="path">The path of the secret.</param>
    /// <returns>True only when metadata confirms a tombstone; false for live, missing or unreadable metadata.</returns>
    Task<bool> IsTombstone(string path);

    /// <summary>
    /// Reads the current version of a secret.
    /// </summary>
    /// <param name="path">The path of the secret.</param>
    /// <returns>The <see cref="VaultSecret"/>, or null when it does not exist.</returns>
    Task<VaultSecret?> Read(string path);

    /// <summary>
    /// Writes a secret only when the path has no version yet.
    /// </summary>
    /// <param name="path">The path of the secret.</param>
    /// <param name="data">The data to write.</param>
    /// <returns>The version written, or null when the path already had a version and nothing was written.</returns>
    Task<int?> WriteIfAbsent(string path, IReadOnlyDictionary<string, string> data);

    /// <summary>
    /// Removes a secret with all its versions.
    /// </summary>
    /// <param name="path">The path of the secret.</param>
    /// <returns>A <see cref="Task"/> for continuation.</returns>
    Task Remove(string path);
}
