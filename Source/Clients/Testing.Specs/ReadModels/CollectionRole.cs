// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Child read model for a collection role with a discriminator set via <see cref="SetValueAttribute{TEvent}"/>.
/// </summary>
/// <param name="Id">The role identifier used as the key.</param>
/// <param name="Name">Role name.</param>
/// <param name="RoleType">The role type, set to a constant depending on which event created the entry.</param>
public sealed record CollectionRole(
    [Key] Guid Id,
    string Name,
    [SetValue<UIRoleAdded>(CollectionRoleType.UIRole)]
    [SetValue<SystemRoleAdded>(CollectionRoleType.SystemRole)]
    CollectionRoleType RoleType);
