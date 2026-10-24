// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A secondary event that can create an actor through a child property mapping.
/// </summary>
/// <param name="ActorId">The actor identifier.</param>
/// <param name="DisplayName">The actor display name.</param>
[EventType]
public record SliceUiActorImported(Guid ActorId, string DisplayName);
