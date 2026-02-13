// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when a projection is missing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingProjection"/> class.
/// </remarks>
/// <param name="id">The <see cref="ProjectionId"/> for the missing projection.</param>
public class MissingProjection(ProjectionId id) : Exception($"Missing projection with id {id.Value}")
{
}
