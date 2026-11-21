// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents an Ed25519 public key an event store uses to sign webhook deliveries.
/// </summary>
/// <param name="KeyId">The identifier of the key.</param>
/// <param name="Status">Whether the key is the current one or is only overlapping.</param>
/// <param name="PublicKey">The <c language="text">whpk_</c> prefixed Base64 encoding of the raw 32-byte public key.</param>
/// <param name="CreatedAt">When the key was created.</param>
/// <param name="OverlapEndsAt">When the overlap ends, or <see langword="null"/> when the key has no overlap end.</param>
public record WebhookSigningPublicKey(
    string KeyId,
    WebhookSigningKeyStatus Status,
    string PublicKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? OverlapEndsAt);
