// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Keeps private key state and failures behind the public API boundary.
/// </summary>
internal static class WebhookSigningKeyOperations
{
    /// <summary>
    /// Checks whether an optional overlap is finite and within the supported bounds.
    /// </summary>
    /// <param name="seconds">The overlap in seconds, or null for the default.</param>
    /// <returns>Whether the overlap is valid.</returns>
    internal static bool IsValidOverlap(double? seconds) => seconds is null ||
        (double.IsFinite(seconds.Value) && seconds.Value >= 0 && seconds.Value <= WebhookSigningKeyOverlap.Maximum.Value.TotalSeconds);

    /// <summary>
    /// Gets public verification metadata while masking key-ring failures.
    /// </summary>
    /// <param name="eventStore">The event store whose keys to read.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The active and unexpired overlap public keys.</returns>
    internal static async Task<IEnumerable<WebhookSigningPublicKey>> GetPublicKeys(EventStoreName eventStore, IGrainFactory grainFactory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventStore?.Value)) throw new WebhookSigningUnavailable();
            var keys = await grainFactory.GetGrain<IWebhookSigningKeyRing>(eventStore).GetSigningKeys();
            return WebhookSigningPublicKeyConverters.ToPublicKeys(keys, DateTimeOffset.UtcNow);
        }
        catch
        {
            throw new WebhookSigningUnavailable();
        }
    }

    /// <summary>
    /// Rotates the addressed key ring while masking private-state failures.
    /// </summary>
    /// <param name="eventStore">The event store whose key to rotate.</param>
    /// <param name="overlapSeconds">The overlap in seconds, or null for the default.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>A task representing the rotation.</returns>
    internal static async Task Rotate(EventStoreName eventStore, double? overlapSeconds, IGrainFactory grainFactory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventStore?.Value) || !IsValidOverlap(overlapSeconds)) throw new WebhookSigningUnavailable();
            var overlap = overlapSeconds is null ? WebhookSigningKeyOverlap.Default : new WebhookSigningKeyOverlap(TimeSpan.FromSeconds(overlapSeconds.Value));
            await grainFactory.GetGrain<IWebhookSigningKeyRing>(eventStore).Rotate(overlap);
        }
        catch
        {
            throw new WebhookSigningUnavailable();
        }
    }
}
