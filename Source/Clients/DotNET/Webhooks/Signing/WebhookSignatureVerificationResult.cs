// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents the outcome of verifying a webhook delivery.
/// </summary>
/// <param name="Failure">Why the delivery was not accepted, or <see cref="WebhookSignatureFailure.None"/>.</param>
/// <param name="KeyId">The identifier of the configured key whose signature matched, when valid.</param>
public record WebhookSignatureVerificationResult(WebhookSignatureFailure Failure, string? KeyId = null)
{
    /// <summary>
    /// Gets a value indicating whether the delivery is authentic and recent.
    /// </summary>
    public bool IsValid => Failure == WebhookSignatureFailure.None;
}
