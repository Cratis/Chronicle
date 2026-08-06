// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a polymorphic children collection projected from an event that does not itself
/// carry any discriminator information — the discriminator must be inferred from
/// <see cref="UserExperienceActor"/> being the only implementation of <see cref="IActor"/>.
/// </summary>
/// <param name="Id">The slice identifier used as the key.</param>
/// <param name="Actors">The actors on the slice.</param>
[Passive]
[FromEvent<SliceCreated>]
public sealed record SliceWithActors(
    [Key] Guid Id,
    [ChildrenFrom<SliceUiActorUpdated>(key: nameof(SliceUiActorUpdated.ActorId), parentKey: nameof(SliceUiActorUpdated.SliceId))]
    IReadOnlyList<IActor> Actors);
