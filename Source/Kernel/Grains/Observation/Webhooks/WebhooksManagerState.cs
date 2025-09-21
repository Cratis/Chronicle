// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the state of the <see cref="WebhooksManager"/>.
/// </summary>
public class WebhooksManagerState
{
    /// <summary>
    /// Gets or sets the webhook definitions.
    /// </summary>
    public IEnumerable<WebhookDefinition> Webhooks { get; set; } = [];
}
