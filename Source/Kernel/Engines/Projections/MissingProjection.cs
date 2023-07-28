// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Exception that gets thrown when a projection is missing.
/// </summary>
public class MissingProjection : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingProjection"/> class.
    /// </summary>
    /// <param name="id">The <see cref="ProjectionId"/> for the missing projection.</param>
    public MissingProjection(ProjectionId id) : base($"Missing projection with id {id.Value}") { }
}
