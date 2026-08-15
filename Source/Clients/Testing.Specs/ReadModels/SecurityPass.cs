// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model holding a nested badge that one event issues and another revokes.
/// </summary>
/// <param name="Id">Pass identifier (the event source id).</param>
/// <param name="Badge">The badge currently held, or <see langword="null"/> once it has been revoked.</param>
[Passive]
[FromEvent<SecurityBadgeIssued>]
public sealed record SecurityPass(
    [Key] Guid Id,
    [Nested] SecurityBadge? Badge);
