// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Defines the request for registering projections.
/// </summary>
[ProtoContract]
public class ProjectionWatchRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the projection id to observe changes for.
    /// </summary>
    [ProtoMember(2)]
    public string ProjectionId { get; set; }
}
