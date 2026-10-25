// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Disabled generations do not expose even an existing Building intent for recovery.
/// </summary>
public class when_generations_are_disabled : given.a_generation_build_recovery
{
    /// <summary>
    /// Disables recovery after a real candidate reservation.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _enablement.Enabled.Returns(false);
        _keys.ClearReceivedCalls();
    }

    /// <summary>
    /// Attempts recovery while generations are disabled.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// Disabled generations have no recovery outcomes.
    /// </summary>
    [Fact] void should_return_no_builds() => _result.ShouldBeEmpty();

    /// <summary>
    /// Recovery does not resolve a materialization while disabled.
    /// </summary>
    [Fact] void should_not_resolve_a_key() => _keys.DidNotReceive().For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
}
