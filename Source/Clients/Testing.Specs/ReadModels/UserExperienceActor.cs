// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The only implementation of <see cref="IActor"/> — a discriminator for it must be inferred
/// automatically since there is no <see cref="SetValueAttribute{TEvent}"/> to set one explicitly.
/// </summary>
/// <param name="ActorId">The actor identifier used as the key.</param>
/// <param name="DisplayName">Display name of the actor.</param>
[DerivedType("userExperience", typeof(IActor))]
public sealed record UserExperienceActor(
    [Key] Guid ActorId,
    [SetFrom<SliceUiActorUpdated>(nameof(SliceUiActorUpdated.DisplayName))] string DisplayName) : IActor;
