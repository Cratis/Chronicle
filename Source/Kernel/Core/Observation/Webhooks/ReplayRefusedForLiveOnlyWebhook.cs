// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when replayed events reach a webhook observer that is live-only.
/// </summary>
/// <param name="key">The <see cref="ObserverKey"/> of the webhook observer.</param>
public class ReplayRefusedForLiveOnlyWebhook(ObserverKey key)
    : Exception($"Webhook observer '{key}' uses delivery envelope v1, which is live-only; replayed events are not delivered over HTTP. The partition was parked.");
