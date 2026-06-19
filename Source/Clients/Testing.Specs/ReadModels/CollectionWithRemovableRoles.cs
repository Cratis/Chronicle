// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model projected from <see cref="CollectionCreated"/> with role children that can be removed
/// via the child's class-level <see cref="RemovedWithAttribute{TEvent}"/>.
/// </summary>
/// <param name="Id">Collection identifier.</param>
/// <param name="Roles">Role children keyed by <see cref="SystemRoleAdded.RoleId"/>.</param>
[Passive]
[FromEvent<CollectionCreated>]
public sealed record CollectionWithRemovableRoles(
    [Key] Guid Id,
    [ChildrenFrom<SystemRoleAdded>(key: nameof(SystemRoleAdded.RoleId), parentKey: nameof(SystemRoleAdded.CollectionId), identifiedBy: nameof(RemovableCollectionRole.Id))]
    IReadOnlyList<RemovableCollectionRole> Roles);
