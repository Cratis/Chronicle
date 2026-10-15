// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

/// <summary>
/// A child identified by a nested member.
/// </summary>
/// <param name="Identity">The child's identity and current topic.</param>
public record NestedIdentityChild(NestedIdentityBrief Identity);
