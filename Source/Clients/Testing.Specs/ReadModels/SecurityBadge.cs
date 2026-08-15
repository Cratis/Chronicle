// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Nested single-object badge on a <see cref="SecurityPass"/>, cleared as a whole by the class-level
/// <see cref="ClearWithAttribute{TEvent}"/>.
/// </summary>
/// <param name="BadgeNumber">The number printed on the badge.</param>
/// <param name="Zone">The zone the badge admits to.</param>
[FromEvent<SecurityBadgeIssued>]
[ClearWith<SecurityBadgeRevoked>]
public record SecurityBadge(string BadgeNumber, string Zone);
