// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Generations.Bindings;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Recovery is empty before the store's generation floor is established.
/// </summary>
public class when_the_generation_floor_is_not_established : given.a_generation_build_recovery
{
    /// <summary>
    /// Models an uninitialized generation floor.
    /// </summary>
    void Establish() => _keys.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>())
        .ThrowsAsync(new GenerationFloorNotEstablished(Store));

    /// <summary>
    /// Attempts catalog recovery without a current materialization key.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// The missing floor is not a recovery error.
    /// </summary>
    [Fact] void should_return_no_builds() => _result.ShouldBeEmpty();
}
