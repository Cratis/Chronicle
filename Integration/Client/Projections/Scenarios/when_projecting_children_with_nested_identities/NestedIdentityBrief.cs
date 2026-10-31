// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

/// <summary>
/// The identity and topic of a recorded brief.
/// </summary>
/// <param name="Id">The brief identifier.</param>
/// <param name="Topic">The brief topic.</param>
public record NestedIdentityBrief(Guid Id, string Topic);
