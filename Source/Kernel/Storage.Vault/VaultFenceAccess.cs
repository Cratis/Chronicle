// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Native versioned KV access. Only definite CAS mismatches may be retried; ambiguous writes escape unchanged.
/// </summary>
internal sealed class VaultFenceAccess
{
    readonly IVaultClient _client;
    readonly string _mount;
    readonly Random _random;
    readonly Func<TimeSpan, Task> _delay;
    readonly Lazy<Task<EncryptionKeyFenceGuarantees>> _probe;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultFenceAccess"/> class and installs the consistency header.
    /// </summary>
    /// <param name="client">The Vault client.</param>
    /// <param name="mount">The KV v2 mount.</param>
    /// <param name="upgradeCheck">The capability check.</param>
    /// <param name="random">The jitter source.</param>
    /// <param name="delay">The retry delay.</param>
    internal VaultFenceAccess(IVaultClient client, string mount, VaultKeyStoreUpgradeCheck upgradeCheck, Random? random, Func<TimeSpan, Task>? delay)
    {
        _client = client;
        _mount = mount;

        // The public capability getter is synchronous; do not capture its caller's synchronization context.
        _probe = new(() => Task.Run(() => upgradeCheck.ProbeFenceGuarantees(this)));
        _random = random ?? Random.Shared;
        _delay = delay ?? (_ => Task.Delay(_));
        var previous = client.Settings.BeforeApiRequestAction;
        client.Settings.BeforeApiRequestAction = (httpClient, request) =>
        {
            previous?.Invoke(httpClient, request);
            request.Headers.Remove("X-Vault-Inconsistent");
            request.Headers.Add("X-Vault-Inconsistent", "forward-active-node");
        };
    }

    /// <summary>Gets the guarantees proven by the server probe.</summary>
    internal EncryptionKeyFenceGuarantees Guarantees => Probe().GetAwaiter().GetResult();

    /// <summary>Get bounded exponential jitter, injectable in specifications.</summary>
    /// <param name="attempt">The attempt number.</param>
    /// <param name="random">The jitter source.</param>
    /// <returns>The delay.</returns>
    internal static TimeSpan BackoffDelay(int attempt, Random random)
    {
        var milliseconds = 1 << Math.Clamp(attempt, 0, 5);
        return TimeSpan.FromMilliseconds(milliseconds + random.Next(milliseconds));
    }

    /// <summary>Probe the server once per client.</summary>
    /// <returns>The proven guarantees.</returns>
    internal Task<EncryptionKeyFenceGuarantees> Probe() => _probe.Value;

    /// <summary>Refuse an unverified server.</summary>
    /// <param name="identifier">The subject.</param>
    /// <returns>The continuation.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The server cannot guarantee strong fences.</exception>
    internal async Task Require(EncryptionKeyIdentifier identifier)
    {
        if (await Probe() == EncryptionKeyFenceGuarantees.None) throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <summary>Read a secret and its native CAS version.</summary>
    /// <param name="path">The secret path.</param>
    /// <returns>The secret or null when absent.</returns>
    internal async Task<(Dictionary<string, object> Data, int Version)?> Read(string path)
    {
        try
        {
            var secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mount);
            return (new(secret.Data.Data, StringComparer.Ordinal), secret.Data.Metadata.Version);
        }
        catch (VaultApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Read a validated fence, refusing unavailable strong reads.</summary>
    /// <param name="path">The secret path.</param>
    /// <param name="identifier">The subject.</param>
    /// <returns>The fence and native version, or null.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">A strong read was unavailable.</exception>
    internal async Task<(EncryptionKeyErasure Fence, int Version)?> ReadFence(string path, EncryptionKeyIdentifier identifier)
    {
        await Require(identifier);
        try
        {
            var row = await Read(path);
            return row is null ? null : (VaultFenceDocument.Read(identifier, row.Value.Data), row.Value.Version);
        }
        catch (Exception exception) when (exception is VaultApiException or HttpRequestException or TimeoutException or OperationCanceledException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <summary>Compare-exchange once. Only a definite CAS conflict returns null.</summary>
    /// <param name="path">The secret path.</param>
    /// <param name="data">The new data.</param>
    /// <param name="version">The expected native version.</param>
    /// <returns>The new version, or null on conflict.</returns>
    internal async Task<int?> Write(string path, Dictionary<string, object> data, int version)
    {
        try
        {
            var result = await _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data, checkAndSet: version, mountPoint: _mount);
            return result.Data.Version;
        }
        catch (VaultApiException exception) when (VaultEncryptionKeyStorage.IsCheckAndSetMismatch(exception))
        {
            return null;
        }
    }

    /// <summary>Seal and compare-exchange a fence.</summary>
    /// <param name="path">The secret path.</param>
    /// <param name="fence">The next fence.</param>
    /// <param name="version">The expected native version.</param>
    /// <returns>The new version, or null on conflict.</returns>
    internal Task<int?> WriteFence(string path, EncryptionKeyErasure fence, int version) => Write(path, VaultFenceDocument.Write(fence), version);

    /// <summary>Destroy historical versions without resetting CurrentVersion.</summary>
    /// <param name="path">The secret path.</param>
    /// <param name="through">The inclusive native version boundary.</param>
    /// <returns>The continuation.</returns>
    internal Task Destroy(string path, int through) => _client.V1.Secrets.KeyValue.V2.DestroySecretVersionsAsync(path, Enumerable.Range(1, through).ToList(), mountPoint: _mount);

    /// <summary>Verify every historical version under a tombstone was physically destroyed.</summary>
    /// <param name="path">The secret path.</param>
    /// <param name="through">The inclusive native version boundary.</param>
    /// <returns>Whether all historical versions are destroyed.</returns>
    internal async Task<bool> IsDestroyed(string path, int through)
    {
        var metadata = await _client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(path, mountPoint: _mount);
        return metadata.Data.Versions.Where(_ => int.Parse(_.Key, CultureInfo.InvariantCulture) <= through).All(_ => _.Value.Destroyed);
    }

    /// <summary>Delay a definite conflict retry.</summary>
    /// <param name="attempt">The attempt number.</param>
    /// <returns>The continuation.</returns>
    internal Task Backoff(int attempt) => _delay(BackoffDelay(attempt, _random));
}
