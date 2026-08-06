// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for adding or updating a <see cref="UserExperienceActor"/> on a <see cref="SliceWithActors"/>.
/// </summary>
/// <param name="SliceId">The parent slice identifier.</param>
/// <param name="ActorId">The actor identifier.</param>
/// <param name="DisplayName">Display name of the actor.</param>
[EventType]
public record SliceUiActorUpdated(Guid SliceId, Guid ActorId, string DisplayName);
