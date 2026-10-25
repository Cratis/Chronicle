// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// A crash after reservation recovers the original persisted intent without admitting a writer.
/// </summary>
public class when_a_building_reservation_exists : given.a_generation_build_recovery
{
    /// <summary>
    /// Persists the candidate without starting an owner.
    /// </summary>
    async Task Establish() => _reserved = await Reserve(_request);

    /// <summary>
    /// Resolves recoverable builds.
    /// </summary>
    async Task Because() => _result = await Resolve();

    /// <summary>
    /// Only the reserved Building generation is returned.
    /// </summary>
    [Fact] void should_return_the_original_record() => _result.Single().Record.ShouldEqual(_reserved);

    /// <summary>
    /// The immutable request is preserved in full.
    /// </summary>
    [Fact] void should_recover_the_original_request() => _result.Single().Request.ShouldEqual(_request);

    /// <summary>
    /// Recovery preserves the caller's retry identity.
    /// </summary>
    [Fact] void should_preserve_the_operation() => _result.Single().Request!.Operation.ShouldEqual(_request.Operation);

    /// <summary>
    /// Recovery preserves the original request time rather than assigning a restart time.
    /// </summary>
    [Fact] void should_preserve_the_request_time() => _result.Single().Request!.At.ShouldEqual(_request.At);

    /// <summary>
    /// A valid persisted intent is not refused.
    /// </summary>
    [Fact] void should_not_refuse_recovery() => _result.Single().Refusal.ShouldBeNull();

    /// <summary>
    /// Recovery leaves the serving generation unchanged.
    /// </summary>
    [Fact] async Task should_preserve_the_active_generation() =>
        (await _catalog.Get(Store, _key))!.Active.ShouldEqual(MaterializationGeneration.First);

    /// <summary>
    /// Recovery does not advance the candidate lifecycle.
    /// </summary>
    [Fact] async Task should_preserve_the_building_record() =>
        (await _catalog.Get(Store, _key))!.Generations.Single(_ => _.Identity == _reserved.Identity).ShouldEqual(_reserved);

    /// <summary>
    /// Recovery does not reserve another generation.
    /// </summary>
    [Fact] async Task should_not_allocate_a_generation() =>
        (await _catalog.Get(Store, _key))!.Generations.Count.ShouldEqual(2);

    /// <summary>
    /// Recovery does not acquire writer authority for the reserved operation.
    /// </summary>
    [Fact] async Task should_not_admit_a_writer() =>
        (await _catalog.GetAdmitted(Store, new GenerationObserverBinding(_key, _reserved.Identity, _request.Execution.EventSequence, _request.Execution.Revision))).ShouldBeNull();
}
