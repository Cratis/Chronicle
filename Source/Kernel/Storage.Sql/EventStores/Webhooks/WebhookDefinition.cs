// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Represents the registration of a single webhook.
/// </summary>
public class WebhookDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the webhook.
    /// </summary>
    [Key]
    public required string Id { get; set; }

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
    [Json]
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the <see cref="WebhookTarget"/> the webhook is for.
    /// </summary>
    [Json]
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
