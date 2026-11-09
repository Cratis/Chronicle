// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;

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

    static string? AsText(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null
    };
}
