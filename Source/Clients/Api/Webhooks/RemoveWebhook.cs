// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a command to remove a webhook.
/// </summary>
public class RemoveWebhook
{
    /// <summary>
    /// Gets or sets the webhook ID to remove.
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;
}
