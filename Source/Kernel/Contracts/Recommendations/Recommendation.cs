// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Recommendations;

/// <summary>
/// Represents information about a recommendation.
/// </summary>
[ProtoContract]
public class Recommendation
{
    /// <summary>
    /// The unique identifier of the recommendation.
    /// </summary>
    [ProtoMember(1)]
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the recommendation.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; }

    /// <summary>
    /// The details of the recommendation.
    /// </summary>
    [ProtoMember(3)]
    public string Description { get; set; }

    /// <summary>
    /// The type of the recommendation.
    /// </summary>
    [ProtoMember(4)]
    public string Type { get; set; }

    /// <summary>
    /// When the recommendation occurred.
    /// </summary>
    [ProtoMember(5)]
    public SerializableDateTimeOffset Occurred { get; set; }
}
