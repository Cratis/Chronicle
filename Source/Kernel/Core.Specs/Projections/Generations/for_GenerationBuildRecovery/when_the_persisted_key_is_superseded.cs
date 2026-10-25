// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// An intent for a different key must never be rebuilt from the current definition.
/// </summary>
public class when_the_persisted_key_is_superseded : given.a_generation_build_recovery
{
    /// <summary>
    /// Models a persisted request whose materialization incarnation is no longer current.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _builds = Substitute.For<IGenerationBuildStorage>();
        var stale = _request with { Key = _key with { Incarnation = new(Guid.NewGuid()) } };
        _builds.GetBuildRequest(Store, _key, _reserved.Identity.Generation).Returns(Task.FromResult<GenerationBuildRequest?>(stale));
    }

    /// <summary>
    /// Reads the mismatched intent.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// The stale request is refused explicitly.
    /// </summary>
    [Fact] void should_refuse_the_superseded_key() => _result.Single().Refusal.ShouldEqual(GenerationBuildRecoveryRefusal.KeySuperseded);

    /// <summary>
    /// The stale request is not exposed as recoverable.
    /// </summary>
    [Fact] void should_not_return_a_recoverable_request() => _result.Single().Request.ShouldBeNull();

    /// <summary>
    /// The refusal identifies the original Building record.
    /// </summary>
    [Fact] void should_identify_the_catalog_record() => _result.Single().Record.ShouldEqual(_reserved);
}
