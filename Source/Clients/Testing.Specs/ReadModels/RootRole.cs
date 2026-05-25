// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root-level read model projected from multiple role events, each setting a different constant <see cref="RoleType"/>.
/// Reproduces the multi-[SetValue] bug: the second and subsequent [SetValue] declarations were silently ignored.
/// </summary>
/// <param name="Id">The role identifier.</param>
/// <param name="Name">The role name.</param>
/// <param name="RoleType">The role type — set to a constant determined by which event triggered the projection.</param>
[Passive]
[FromEvent<UIRoleAdded>(key: nameof(UIRoleAdded.RoleId))]
[FromEvent<SystemRoleAdded>(key: nameof(SystemRoleAdded.RoleId))]
public sealed record RootRole(
    [Key] Guid Id,
    string Name,
    [SetValue<UIRoleAdded>(CollectionRoleType.UIRole)]
    [SetValue<SystemRoleAdded>(CollectionRoleType.SystemRole)]
    CollectionRoleType RoleType);
