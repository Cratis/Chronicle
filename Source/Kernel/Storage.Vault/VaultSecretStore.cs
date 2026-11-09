// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents an implementation of <see cref="IVaultSecretStore"/> over an <see cref="IVaultClient"/>.
/// </summary>
/// <param name="vaultClient">The <see cref="IVaultClient"/> to talk to Vault through.</param>
/// <param name="mountPointName">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
internal class VaultSecretStore(IVaultClient vaultClient, string? mountPointName = null) : IVaultSecretStore
{
    const string DefaultMountPoint = "secret";

    readonly string _mountPoint = string.IsNullOrEmpty(mountPointName) ? DefaultMountPoint : mountPointName;

    /// <inheritdoc/>
    public async Task<bool> IsKv2()
    {
        var mount = await vaultClient.V1.System.GetSecretBackendAsync(_mountPoint);

        // Vault reports both KV versions as type "kv"; only options.version distinguishes v2.
        return mount.Data.Type == VaultSharp.V1.SecretsEngines.SecretsEngineType.KeyValueV1 &&
            mount.Data.Options is not null &&
            mount.Data.Options.TryGetValue("version", out var version) && version switch
            {
                string text => text == "2",
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() == "2",
                _ => false
            };
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveVersionIfUnchanged(string path, VaultSecret expected, Func<Task<bool>> canRemove)
    {
        if (await Read(path) is not { } current || current.Version != expected.Version || !VaultLegacyLayout.SameContent(expected, current) || !await canRemove())
        {
            return false;
        }

        // Never destroy metadata here: a writer may have added a newer version since the read.
        await vaultClient.V1.Secrets.KeyValue.V2.DeleteSecretVersionsAsync(path, [expected.Version], mountPoint: _mountPoint);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> List(string path)
    {
        try
        {
            var list = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path, mountPoint: _mountPoint);
            return [.. list.Data.Keys];
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsTombstone(string path)
    {
        try
        {
            var metadata = (await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(path, mountPoint: _mountPoint)).Data;
            return AllVersionsDeleted(metadata);
        }
        catch (Exception)
        {
            // A missing value alone proves nothing: unreadable or absent metadata must stay pending.
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<VaultSecret?> Read(string path)
    {
        try
        {
            var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mountPoint);
            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            var onlyText = true;
            foreach (var (field, value) in secret.Data.Data ?? new Dictionary<string, object>())
            {
                if (AsText(value) is { } text)
                {
                    data[field] = text;
                }
                else
                {
                    onlyText = false;
                }
            }

            return new(secret.Data.Metadata.Version, data, onlyText);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<int?> WriteIfAbsent(string path, IReadOnlyDictionary<string, string> data)
    {
        try
        {
            var written = await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path,
                data.ToDictionary(field => field.Key, field => (object)field.Value, StringComparer.Ordinal),
                checkAndSet: 0,
                mountPoint: _mountPoint);
            return written.Data.Version;
        }
        catch (VaultApiException ex) when (VaultEncryptionKeyStorage.IsCheckAndSetMismatch(ex))
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public Task Remove(string path) => vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(path, mountPoint: _mountPoint);

    static bool AllVersionsDeleted(FullSecretMetadata metadata) =>
        metadata.CurrentVersion > 0 &&
        metadata.Versions is { Count: > 0 } versions &&
        versions.ContainsKey(metadata.CurrentVersion.ToString(CultureInfo.InvariantCulture)) &&
        versions.Values.All(IsDeletedOrDestroyed);

    static bool IsDeletedOrDestroyed(SecretMetadata? version) =>
        version is not null &&
        (version.Destroyed ||
            (DateTimeOffset.TryParse(version.DeletionTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var deleted) && deleted <= DateTimeOffset.UtcNow));

    static string? AsText(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null
    };
}
