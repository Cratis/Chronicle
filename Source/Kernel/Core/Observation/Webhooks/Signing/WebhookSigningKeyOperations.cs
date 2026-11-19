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
    internal static bool IsValidOverlap(double? seconds) => seconds is null ||
        (double.IsFinite(seconds.Value) && seconds.Value >= 0 && seconds.Value <= WebhookSigningKeyOverlap.Maximum.Value.TotalSeconds);

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
