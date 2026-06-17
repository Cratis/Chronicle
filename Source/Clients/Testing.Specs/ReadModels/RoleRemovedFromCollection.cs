// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for removing a role from a collection.
/// </summary>
/// <param name="CollectionId">The collection identifier.</param>
/// <param name="RoleId">The role identifier.</param>
[EventType]
public record RoleRemovedFromCollection(Guid CollectionId, Guid RoleId);
