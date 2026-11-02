// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Represents the highest available eligibility tier and its silos.
/// </summary>
/// <param name="Tier">The selected tier.</param>
/// <param name="Silos">The compatible silos in that tier.</param>
public record EligibleSilos(RoleEligibilityTier Tier, IReadOnlyList<SiloAddress> Silos);
