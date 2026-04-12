// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A feature child model with nested slices.
/// </summary>
/// <param name="Id">The feature identifier used as the key.</param>
/// <param name="Name">Feature name.</param>
/// <param name="Slices">Nested slice children keyed by <see cref="SliceAdded.FeatureId"/>.</param>
public record FeatureChild(
    [Key] Guid Id,
    string Name,

    [ChildrenFrom<SliceAdded>(key: nameof(SliceAdded.SliceId), identifiedBy: nameof(SliceChild.Id), parentKey: nameof(SliceAdded.FeatureId))]
    IEnumerable<SliceChild> Slices);
