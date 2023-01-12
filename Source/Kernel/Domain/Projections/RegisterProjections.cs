// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the payload for registering projections.
/// </summary>
/// <param name="Projections">Collection of <see cref="ProjectionRegistration"/>.</param>
public record RegisterProjections(IEnumerable<ProjectionRegistration> Projections);
