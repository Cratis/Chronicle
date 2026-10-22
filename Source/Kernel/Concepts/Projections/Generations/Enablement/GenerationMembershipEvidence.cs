// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>Uniform-membership diagnostics, not proof of old-binary exclusion.</summary>
/// <param name="IsUniform">Whether every member satisfies the requested floor.</param>
/// <param name="Reason">The diagnostic explanation.</param>
public record GenerationMembershipEvidence(bool IsUniform, string Reason);
