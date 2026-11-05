// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents an implementation of <see cref="IWebhookSigningKeyRing"/>.
/// </summary>
/// <remarks>
/// The grain is the only writer of the signing keys and is not reentrant, so rotations serialize per event store.
/// A key is never returned before it is persisted. Events carry no key material.
/// </remarks>
/// <param name="encryption">The <see cref="IEncryption"/> protecting private keys at rest.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/>.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.WebhookSigningKeys)]
public class WebhookSigningKeyRing(
    IEncryption encryption,
    ILogger<WebhookSigningKeyRing> logger,
    TimeProvider? timeProvider = null) : Grain<WebhookSigningKeyRingState>, IWebhookSigningKeyRing
{
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    EventStoreName _eventStore = EventStoreName.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStore = this.GetPrimaryKeyString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WebhookSigningKey>> GetSigningKeys()
    {
        var now = _timeProvider.GetUtcNow();
        await RetireExpired(now);
        if (ActiveKey() is null)
        {
            await Generate(now);
        }

        return [.. State.Keys.Where(key => key.Status == WebhookSigningKeyStatus.Active), .. State.Keys.Where(key => key.Status == WebhookSigningKeyStatus.Overlap)];
    }

    /// <inheritdoc/>
    public async Task<WebhookSigningKey> Rotate(WebhookSigningKeyOverlap overlap)
    {
        if (overlap.Value < TimeSpan.Zero || overlap.Value > WebhookSigningKeyOverlap.Maximum.Value)
        {
            throw new InvalidWebhookSigningKeyOverlap(overlap);
        }

        var now = _timeProvider.GetUtcNow();
        await RetireExpired(now);
        var active = ActiveKey();
        if (active is not null)
        {
            await Supersede(active, now + overlap.Value);
            await RetireExpired(now);
        }

        return await Generate(now);
    }

    static bool IsStoreWideEd25519(WebhookSigningKey key) => key.Algorithm == WebhookSigningAlgorithm.Ed25519 && key.Webhook is null;

    WebhookSigningKey? ActiveKey() => State.Keys.FirstOrDefault(key => key.Status == WebhookSigningKeyStatus.Active && IsStoreWideEd25519(key));

    async Task RetireExpired(DateTimeOffset now)
    {
        foreach (var key in State.Keys.Where(key => key.Status == WebhookSigningKeyStatus.Overlap && key.OverlapEndsAt <= now).ToArray())
        {
            await Append(key.KeyId, new WebhookSigningKeyRetired(key.KeyId));
            await Persist(key with { Status = WebhookSigningKeyStatus.Retired, RetiredAt = now, EncryptedSecret = EncryptedWebhookSigningSecret.Empty });
            logger.Retired(_eventStore, key.KeyId.Value);
        }
    }

    async Task Supersede(WebhookSigningKey key, DateTimeOffset overlapEndsAt)
    {
        await Append(key.KeyId, new WebhookSigningKeySuperseded(key.KeyId, overlapEndsAt));
        await Persist(key with { Status = WebhookSigningKeyStatus.Overlap, OverlapEndsAt = overlapEndsAt });
        logger.Superseded(_eventStore, key.KeyId.Value, overlapEndsAt);
    }

    async Task<WebhookSigningKey> Generate(DateTimeOffset now)
    {
        var keyId = WebhookSigningKeyId.New();
        var seed = WebhookEd25519Signing.GenerateSeed();
        WebhookSigningKey key;
        try
        {
            var publicKey = WebhookSigningKeys.EncodePublicKey(WebhookEd25519Signing.DerivePublicKey(seed));
            var encrypted = encryption.Encrypt(WebhookSigningKeys.EncodePrivateKey(seed));
            key = new(keyId, WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Active, publicKey, encrypted, null, now, null, null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }

        await Append(keyId, new WebhookSigningKeyGenerated(keyId, key.Algorithm, key.PublicKey, key.Webhook));
        await Persist(key);
        logger.Generated(_eventStore, keyId.Value, key.Algorithm);
        return key;
    }

    async Task Append(WebhookSigningKeyId keyId, object @event)
    {
        var result = await GrainFactory.GetSystemEventSequence(_eventStore).Append(keyId.Value, @event);
        if (!result.IsSuccess)
        {
            throw new WebhookSigningKeyRingAppendFailed(keyId, @event.GetType());
        }
    }

    async Task Persist(WebhookSigningKey key)
    {
        var previous = State.Keys;
        State.Keys = [.. previous.Where(existing => existing.KeyId != key.KeyId), key];
        try
        {
            await WriteStateAsync();
        }
        catch
        {
            State.Keys = previous;
            throw;
        }
    }
}
