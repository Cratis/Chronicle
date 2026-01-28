// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the definition of a reactor.
/// </summary>
[ProtoContract]
public class WebhookDefinition
{
    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the webhook.
    /// </summary>
    [ProtoMember(2)]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets a collection of event types to observe.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the webhook target.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public WebhookTarget Target { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the webhook observer supports replay scenarios.
    /// </summary>
    [ProtoMember(5), DefaultValue(true)]
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the webhook is active or not.
    /// </summary>
    [ProtoMember(6), DefaultValue(true)]
    public bool IsActive { get; set; } = true;
}