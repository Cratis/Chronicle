// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

/// <summary>Authority captured before an HTTP attempt; never contains the target or credentials.</summary>
/// <param name="LifecycleId">The admitted subscription lifecycle.</param>
/// <param name="DefinitionFingerprint">Opaque fingerprint of the definition actually delivered to.</param>
public record WebhookDeliveryAttempt(Guid LifecycleId, string DefinitionFingerprint)
{
    /// <summary>Fingerprints the authoritative, encrypted definition without exposing its contents.</summary>
    /// <param name="definition">The admitted definition.</param>
    /// <returns>An opaque fingerprint.</returns>
    public static string Fingerprint(WebhookDefinition definition) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition))));
}
