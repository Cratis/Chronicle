// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the request for getting event store subscriptions.
/// </summary>
[ProtoContract]
public class GetEventStoreSubscriptionsRequest
{
    /// <summary>
    /// Gets or sets the target event store name.
    /// </summary>
    [ProtoMember(1)]
    public string TargetEventStore { get; set; }
}
