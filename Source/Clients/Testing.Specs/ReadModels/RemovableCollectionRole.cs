// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Child read model for a collection role that is removed via a class-level <see cref="RemovedWithAttribute{TEvent}"/>.
/// </summary>
/// <param name="Id">The role identifier used as the key.</param>
/// <param name="Name">Role name.</param>
[RemovedWith<RoleRemovedFromCollection>(key: nameof(RoleRemovedFromCollection.RoleId))]
public sealed record RemovableCollectionRole(
    [Key] Guid Id,
    string Name);
