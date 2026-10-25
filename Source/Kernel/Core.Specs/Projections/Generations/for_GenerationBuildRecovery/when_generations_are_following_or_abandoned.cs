// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Neither abandoned nor already-following reservations are recovery candidates.
/// </summary>
public class when_generations_are_following_or_abandoned : given.a_generation_build_recovery
{
    /// <summary>
    /// Creates abandoned and following records through real lifecycle transitions.
    /// </summary>
    async Task Establish()
    {
        var abandoned = await Reserve(_request);
        await _catalog.TryAbandon(Store, _key, abandoned.Identity.Generation);
        var following = await Reserve(_request with { Operation = new(Guid.NewGuid()) });
        await _catalog.TryMarkFollowing(Store, _key, following.Identity.Generation);
    }

    /// <summary>
    /// Resolves the current binding's recovery candidates.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// No non-Building generation is returned.
    /// </summary>
    [Fact] void should_return_no_builds() => _result.ShouldBeEmpty();
}
