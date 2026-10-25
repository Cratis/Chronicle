// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// An unbound projection has no recoverable builds.
/// </summary>
public class when_no_binding_exists : given.a_generation_build_recovery
{
    /// <summary>
    /// Selects a projection absent from the real catalog.
    /// </summary>
    void Establish() => _key = _key with { Projection = "unbound" };

    /// <summary>
    /// Resolves the unbound projection.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// No intent or refusal is invented for an absent binding.
    /// </summary>
    [Fact] void should_return_no_builds() => _result.ShouldBeEmpty();
}
