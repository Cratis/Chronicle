// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose document key is a string concept rather than a <see cref="System.Guid"/> — the shape a
/// store has to parse, and the one the in-process harness models in C# instead of writing and reading back.
/// </summary>
/// <param name="Id">The supplier's organization number, which is also its event source id.</param>
/// <param name="Name">The supplier name.</param>
[Passive]
[FromEvent<SupplierOnboarded>]
public record SupplierProfile(
    [Key] OrgNumber Id,
    string Name);
