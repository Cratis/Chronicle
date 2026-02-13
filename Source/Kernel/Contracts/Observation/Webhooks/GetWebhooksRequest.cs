// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the request for getting webhooks.
/// </summary>
[ProtoContract]
public class GetWebhooksRequest
{
    /// <summary>
    /// Gets or sets the event store to get namespaces for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }
}