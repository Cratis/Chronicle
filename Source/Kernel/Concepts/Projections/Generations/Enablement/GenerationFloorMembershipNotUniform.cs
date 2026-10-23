// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>Uniform membership could not be established.</summary>
/// <param name="Reason">The diagnostic explanation.</param>
public sealed record GenerationFloorMembershipNotUniform(string Reason);
