// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
public class WebhookDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="WebhookId"/> of the webhook.
    /// </summary>
    public WebhookId Id { get; set; } = WebhookId.Unspecified;

    /// <summary>
    /// Gets or sets the owner of the webhook.
    /// </summary>
    public WebhookOwner Owner { get; set; } = WebhookOwner.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the webhook is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the event types and key expressions that the webhook subscribes to.
    /// </summary>
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the <see cref="WebhookTarget"/> the webhook is for.
    /// </summary>
    public WebhookTarget Target { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
