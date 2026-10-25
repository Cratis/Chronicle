// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// A Building record without an original intent cannot invent a replacement request.
/// </summary>
public class when_the_persisted_intent_is_missing : given.a_generation_build_recovery
{
    /// <summary>
    /// Models a missing persisted request for a real reserved candidate.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _builds = Substitute.For<IGenerationBuildStorage>();
        _builds.GetBuildRequest(Store, _key, _reserved.Identity.Generation).Returns(Task.FromResult<GenerationBuildRequest?>(null));
    }

    /// <summary>
    /// Reads the missing persisted intent.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// Missing intent is an explicit refusal rather than an empty result.
    /// </summary>
    [Fact] void should_refuse_the_missing_intent() => _result.Single().Refusal.ShouldEqual(GenerationBuildRecoveryRefusal.IntentMissing);

    /// <summary>
    /// No replacement request is synthesized.
    /// </summary>
    [Fact] void should_not_return_a_recoverable_request() => _result.Single().Request.ShouldBeNull();

    /// <summary>
    /// The refusal identifies the candidate requiring attention.
    /// </summary>
    [Fact] void should_identify_the_catalog_record() => _result.Single().Record.ShouldEqual(_reserved);
}
