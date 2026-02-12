// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a system that is capable of comparing <see cref="WebhookDefinition">webhook definitions</see>.
/// </summary>
public interface IWebhookDefinitionComparer
{
    /// <summary>
    /// Compare two <see cref="WebhookDefinition">projection definitions</see>.
    /// </summary>
    /// <param name="webhookKey">The <see cref="WebhookKey"/>.</param>
    /// <param name="first">The first <see cref="WebhookDefinition"/>.</param>
    /// <param name="second">The second <see cref="WebhookDefinition"/>.</param>
    /// <returns>The <see cref="WebhookDefinitionComparisonResult"/>.</returns>
    Task<WebhookDefinitionComparisonResult> Compare(WebhookKey webhookKey, WebhookDefinition first, WebhookDefinition second);
}
