// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for adding a system role to a collection.
/// </summary>
/// <param name="CollectionId">The collection identifier.</param>
/// <param name="RoleId">The role identifier.</param>
/// <param name="Name">Role name.</param>
[EventType]
public record SystemRoleAdded(Guid CollectionId, Guid RoleId, string Name);
