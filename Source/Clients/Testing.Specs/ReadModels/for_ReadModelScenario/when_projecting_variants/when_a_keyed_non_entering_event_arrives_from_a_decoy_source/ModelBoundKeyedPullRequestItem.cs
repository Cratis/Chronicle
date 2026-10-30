// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_a_keyed_non_entering_event_arrives_from_a_decoy_source;

[VariantOf<KeyedWorkItem>]
[EntersOn<PullRequestCreated>]
[FromEvent<KeyedBuildCompleted>(key: nameof(KeyedBuildCompleted.WorkItemId))]
public record ModelBoundKeyedPullRequestItem(
    [property: Key] Guid Id,
    [property: SetFrom<PullRequestCreated>] string PullRequestUrl,
    [property: SetFrom<KeyedBuildCompleted>] string BuildStatus);
