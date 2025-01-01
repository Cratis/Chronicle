// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Recommendations;

/// <summary>
/// Represents the request for getting recommendations.
/// </summary>
[ProtoContract]
public class Perform
{
    /// <summary>
    /// The name of the event store.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// The namespace of the recommendation.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// The unique identifier of the recommendation.
    /// </summary>
    [ProtoMember(3)]
    public Guid RecommendationId { get; set; }
}
