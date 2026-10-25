// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Conflicting persisted claims produce an explicit recovery refusal.
/// </summary>
public class when_the_persisted_intent_is_inconsistent : given.a_generation_build_recovery
{
    /// <summary>
    /// Models the intent reader rejecting inconsistent durable claims.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _builds = Substitute.For<IGenerationBuildStorage>();
        _builds.GetBuildRequest(Store, _key, _reserved.Identity.Generation).ThrowsAsync(new GenerationBuildConflict());
    }

    /// <summary>
    /// Reads the inconsistent persisted intent.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// Only the known conflict is translated to an inconsistency refusal.
    /// </summary>
    [Fact] void should_refuse_the_inconsistent_intent() => _result.Single().Refusal.ShouldEqual(GenerationBuildRecoveryRefusal.IntentInconsistent);

    /// <summary>
    /// The inconsistent intent cannot be dispatched as recoverable.
    /// </summary>
    [Fact] void should_not_return_a_recoverable_request() => _result.Single().Request.ShouldBeNull();

    /// <summary>
    /// The refusal retains the Building record's identity.
    /// </summary>
    [Fact] void should_identify_the_catalog_record() => _result.Single().Record.ShouldEqual(_reserved);
}
