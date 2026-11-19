// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Security;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Signs the exact delivery bytes with the event store's Ed25519 key ring.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="encryption">The private material encryption boundary.</param>
/// <param name="timeProvider">The overlap clock.</param>
[Singleton]
public class WebhookDeliverySigner(IGrainFactory grainFactory, IEncryption encryption, TimeProvider? timeProvider = null) : IWebhookDeliverySigner
{
    /// <inheritdoc/>
    public async Task<WebhookDeliverySignature> Sign(EventStoreName eventStore, string messageId, long unixTimestamp, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageId) || messageId.Contains('.')) throw new WebhookSigningUnavailable();
            cancellationToken.ThrowIfCancellationRequested();
            var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            var keys = (await grainFactory.GetGrain<IWebhookSigningKeyRing>(eventStore).GetSigningKeys())
                .Where(key => IsSigningKey(key, now))
                .OrderBy(key => key.Status)
                .ThenBy(key => key.OverlapEndsAt)
                .ThenBy(key => key.KeyId.Value, StringComparer.Ordinal)
                .ToArray();
            if (keys.Count(key => key.Status == WebhookSigningKeyStatus.Active) != 1) throw new WebhookSigningUnavailable();

            var signatures = new List<string>();
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                signatures.Add(SignWithKey(key, messageId, unixTimestamp, body));
            }

            return new(messageId, unixTimestamp.ToString(CultureInfo.InvariantCulture), string.Join(' ', signatures), keys[0].KeyId.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Never retain an inner exception from decryption, decoding, or signing.
            throw new WebhookSigningUnavailable();
        }
    }

    static bool IsSigningKey(WebhookSigningKey key, DateTimeOffset now) =>
        key.Webhook is null && key.Algorithm == WebhookSigningAlgorithm.Ed25519 && !string.IsNullOrEmpty(key.EncryptedSecret) &&
        (key.Status == WebhookSigningKeyStatus.Active || (key.Status == WebhookSigningKeyStatus.Overlap && key.OverlapEndsAt > now));

    string SignWithKey(WebhookSigningKey key, string messageId, long timestamp, ReadOnlyMemory<byte> body)
    {
        byte[] seed = [];
        try
        {
            if (!WebhookSigningKeys.TryParsePrivateKey(encryption.Decrypt(key.EncryptedSecret), out seed)) throw new WebhookSigningUnavailable();
            if (!WebhookSigningKeys.TryParsePublicKey(key.PublicKey, out var publicKey)) throw new WebhookSigningUnavailable();
            if (!CryptographicOperations.FixedTimeEquals(WebhookEd25519Signing.DerivePublicKey(seed), publicKey)) throw new WebhookSigningUnavailable();
            return WebhookEd25519Signing.Sign(messageId, timestamp, body.Span, seed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }
    }
}
