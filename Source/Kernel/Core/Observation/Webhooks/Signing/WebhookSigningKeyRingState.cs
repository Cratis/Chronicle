// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents the state of the <see cref="WebhookSigningKeyRing"/>.
/// </summary>
public class WebhookSigningKeyRingState
{
    /// <summary>
    /// Gets or sets the signing keys of the event store, in any status.
    /// </summary>
    public IEnumerable<WebhookSigningKey> Keys { get; set; } = [];
}
