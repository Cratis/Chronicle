// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Separates the folded name from the definition's initial seed.
/// </summary>
/// <param name="Name">The event's name.</param>
/// <param name="Seed">The initial value from the definition.</param>
public record InterleavedReadModel(string Name, string Seed);
