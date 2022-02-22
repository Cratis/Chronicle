// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Exception that gets thrown when a <see cref="ProjectionDefinition"/> is missing in the system.
/// </summary>
public class MissingProjectionDefinition : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingProjectionDefinition"/> class.
    /// </summary>
    /// <param name="identifier"><see cref="ProjectionId"/> of the missing identifier.</param>
    public MissingProjectionDefinition(ProjectionId identifier) : base($"Missing projection definition with id '{identifier}'")
    {
    }
}
