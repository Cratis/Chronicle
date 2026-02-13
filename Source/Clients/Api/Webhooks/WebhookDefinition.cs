// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a webhook definition.
/// </summary>
public class WebhookDefinition
{
    /// <summary>
    /// Gets or sets the webhook ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the webhook.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types and their key expressions.
    /// </summary>
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the authorization type.
    /// </summary>
    public Security.AuthorizationType AuthorizationType { get; set; } = Security.AuthorizationType.None;

    /// <summary>
    /// Gets or sets additional headers.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets whether the webhook is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
