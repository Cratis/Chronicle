// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model projected from <see cref="CollectionCreated"/> with role children.
/// Each child sets a constant <see cref="CollectionRole.RoleType"/> based on which event created it.
/// </summary>
/// <param name="Id">Collection identifier.</param>
/// <param name="Roles">Role children keyed by <see cref="CollectionCreated.CollectionId"/>.</param>
[Passive]
[FromEvent<CollectionCreated>]
public sealed record CollectionRoles(
    [Key] Guid Id,
    [ChildrenFrom<UIRoleAdded>(key: nameof(UIRoleAdded.RoleId), parentKey: nameof(UIRoleAdded.CollectionId), identifiedBy: nameof(CollectionRole.Id))]
    [ChildrenFrom<SystemRoleAdded>(key: nameof(SystemRoleAdded.RoleId), parentKey: nameof(SystemRoleAdded.CollectionId), identifiedBy: nameof(CollectionRole.Id))]
    IReadOnlyList<CollectionRole> Roles)
{
    /// <summary>
    /// Gets a value indicating whether this collection has any UI roles.
    /// </summary>
    public bool HasUiRole => Roles?.Any(r => r.RoleType == CollectionRoleType.UIRole) ?? false;

    /// <summary>
    /// Gets a value indicating whether this collection has any system roles.
    /// </summary>
    public bool HasSystemRole => Roles?.Any(r => r.RoleType == CollectionRoleType.SystemRole) ?? false;
}
