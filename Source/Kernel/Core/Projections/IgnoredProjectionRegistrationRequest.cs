// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the request for a recommendation about a projection registration that was ignored for being older than the stored one.
/// </summary>
[DerivedType("150d77e5-41af-4c34-a817-733425422f15")]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Projections.IgnoredProjectionRegistrationRequest")]
public class IgnoredProjectionRegistrationRequest : IRecommendationRequest
{
    /// <summary>
    /// Gets or sets the <see cref="ProjectionId"/> of the projection whose registration was ignored.
    /// </summary>
    [Id(0)]
    public ProjectionId ProjectionId { get; set; } = ProjectionId.Unspecified;

    /// <summary>
    /// Gets or sets the software version of the client whose registration was ignored.
    /// </summary>
    [Id(1)]
    public string IncomingVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the software version the stored definition was registered with.
    /// </summary>
    [Id(2)]
    public string StoredVersion { get; set; } = string.Empty;
}
