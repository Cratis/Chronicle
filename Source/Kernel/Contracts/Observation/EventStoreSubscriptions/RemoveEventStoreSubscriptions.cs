// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the payload for removing event store subscriptions.
/// </summary>
[ProtoContract]
public class RemoveEventStoreSubscriptions
{
    /// <summary>
    /// Gets or sets the target event store name.
    /// </summary>
    [ProtoMember(1)]
    public string TargetEventStore { get; set; }

    /// <summary>
    /// Gets or sets the collection of subscription identifiers to remove.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<string> SubscriptionIds { get; set; } = [];
}
