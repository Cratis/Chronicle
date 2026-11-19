// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents delivery headers whose signature must not enter diagnostics.
/// </summary>
/// <param name="Id">The deterministic message identifier.</param>
/// <param name="Timestamp">The attempt time in Unix seconds.</param>
/// <param name="Signature">The space-separated signatures, active key first.</param>
/// <param name="KeyId">The advisory active key identifier.</param>
public record WebhookDeliverySignature(string Id, string Timestamp, string Signature, string KeyId)
{
    /// <inheritdoc/>
    public override string ToString() => nameof(WebhookDeliverySignature);
}
