// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Holds log messages for <see cref="WebhookSigningKeyRing"/>. Messages never carry key material.
/// </summary>
internal static partial class WebhookSigningKeyRingLogging
{
    [LoggerMessage(LogLevel.Information, "Generated {Algorithm} webhook signing key '{KeyId}' for event store '{EventStore}'")]
    internal static partial void Generated(this ILogger<WebhookSigningKeyRing> logger, EventStoreName eventStore, string keyId, WebhookSigningAlgorithm algorithm);

    [LoggerMessage(LogLevel.Information, "Webhook signing key '{KeyId}' for event store '{EventStore}' superseded, valid until {OverlapEndsAt}")]
    internal static partial void Superseded(this ILogger<WebhookSigningKeyRing> logger, EventStoreName eventStore, string keyId, DateTimeOffset overlapEndsAt);

    [LoggerMessage(LogLevel.Information, "Webhook signing key '{KeyId}' for event store '{EventStore}' retired")]
    internal static partial void Retired(this ILogger<WebhookSigningKeyRing> logger, EventStoreName eventStore, string keyId);
}
