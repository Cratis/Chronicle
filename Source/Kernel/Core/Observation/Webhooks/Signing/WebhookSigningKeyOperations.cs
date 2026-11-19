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
    /// Checks whether a specified overlap is finite and within the supported bounds.
    /// </summary>
    /// <param name="seconds">The specified overlap in seconds.</param>
    /// <returns>Whether the overlap is valid.</returns>
    internal static bool IsValidOverlap(double seconds) =>
        double.IsFinite(seconds) && seconds >= 0 && seconds <= WebhookSigningKeyOverlap.Maximum.Value.TotalSeconds;

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
    /// <param name="useSpecifiedOverlap">Whether to use the specified overlap instead of the default.</param>
    /// <param name="overlapSeconds">The specified overlap in seconds.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>A task representing the rotation.</returns>
    internal static async Task Rotate(EventStoreName eventStore, bool useSpecifiedOverlap, double overlapSeconds, IGrainFactory grainFactory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventStore?.Value)) throw new WebhookSigningUnavailable();
            if (useSpecifiedOverlap && !IsValidOverlap(overlapSeconds)) throw new WebhookSigningUnavailable();
            var overlap = useSpecifiedOverlap ? new WebhookSigningKeyOverlap(TimeSpan.FromSeconds(overlapSeconds)) : WebhookSigningKeyOverlap.Default;
            await grainFactory.GetGrain<IWebhookSigningKeyRing>(eventStore).Rotate(overlap);
        }
        catch
        {
            throw new WebhookSigningUnavailable();
        }
    }
}
