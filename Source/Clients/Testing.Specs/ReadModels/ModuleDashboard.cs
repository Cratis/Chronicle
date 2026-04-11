// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model projected from <see cref="ModuleCreated"/> with nested children.
/// </summary>
/// <param name="Id">Module identifier.</param>
/// <param name="Name">Module name.</param>
/// <param name="Features">Feature children keyed by <see cref="FeatureAdded.ModuleId"/>.</param>
[Passive]
[FromEvent<ModuleCreated>]
public record ModuleDashboard(
    Guid Id,
    string Name,

    [ChildrenFrom<FeatureAdded>(key: nameof(FeatureAdded.FeatureId), identifiedBy: nameof(FeatureChild.Id), parentKey: nameof(FeatureAdded.ModuleId))]
    IEnumerable<FeatureChild> Features);
