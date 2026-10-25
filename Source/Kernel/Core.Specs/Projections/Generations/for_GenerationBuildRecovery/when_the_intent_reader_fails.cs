// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Unexpected storage failures propagate rather than masquerading as recovery refusals.
/// </summary>
public class when_the_intent_reader_fails : given.a_generation_build_recovery
{
    /// <summary>
    /// The original storage failure.
    /// </summary>
    MaterializationBindingsNotSupported _failure;

    /// <summary>
    /// The observed recovery failure.
    /// </summary>
    Exception _error;

    /// <summary>
    /// Configures a non-conflict failure for a real reserved candidate.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _failure = new();
        _builds = Substitute.For<IGenerationBuildStorage>();
        _builds.GetBuildRequest(Store, _key, _reserved.Identity.Generation).ThrowsAsync(_failure);
    }

    /// <summary>
    /// Captures the propagated storage failure.
    /// </summary>
    async Task Because() => _error = await Catch.Exception(Resolve);

    /// <summary>
    /// Recovery does not swallow or translate non-conflict storage failures.
    /// </summary>
    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
}
