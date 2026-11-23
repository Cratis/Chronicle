// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[FromEvent<ModuleAdded>]
public record RecursiveModule(
    [Key] Guid Id,
    [ChildrenFrom<FeatureAddedToModule>(key: nameof(FeatureAddedToModule.FeatureId), parentKey: nameof(FeatureAddedToModule.ModuleId))]
    IEnumerable<RecursiveFeature> Features);
