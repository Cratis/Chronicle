// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery;

/// <summary>
/// Equal generation numbers in different namespaces retain distinct original intents.
/// </summary>
public class when_namespaces_share_a_generation_number : given.a_generation_build_recovery
{
    /// <summary>
    /// The other namespace's original request.
    /// </summary>
    GenerationBuildRequest _otherRequest;

    /// <summary>
    /// The other namespace's recovery outcomes.
    /// </summary>
    IReadOnlyCollection<RecoverableGenerationBuild> _otherResult;

    /// <summary>
    /// The other namespace's reserved record.
    /// </summary>
    MaterializationGenerationRecord _otherReserved;

    /// <summary>
    /// Reserves the same operation identity and generation number in two namespaces.
    /// </summary>
    async Task Establish()
    {
        _reserved = await Reserve(_request);
        _otherRequest = _request with { Key = _key with { Namespace = "other" }, At = _request.At.AddHours(1) };
        await Adopt(_otherRequest.Key);
        _otherReserved = await Reserve(_otherRequest);
    }

    /// <summary>
    /// Resolves each namespace independently.
    /// </summary>
    async Task Because()
    {
        _result = await Resolve();
        _otherResult = await new GenerationBuildRecovery(_enablement, _keys, _catalog, _builds).Resolve(Store, _otherRequest.Key.Namespace, _key.Projection);
    }

    /// <summary>
    /// The reservations really share the same generation number.
    /// </summary>
    [Fact] void should_share_a_generation_number() => _otherReserved.Identity.Generation.ShouldEqual(_reserved.Identity.Generation);

    /// <summary>
    /// The first namespace recovers only its own request.
    /// </summary>
    [Fact] void should_recover_the_first_namespace_request() => _result.Single().Request.ShouldEqual(_request);

    /// <summary>
    /// The second namespace recovers only its own request.
    /// </summary>
    [Fact] void should_recover_the_second_namespace_request() => _otherResult.Single().Request.ShouldEqual(_otherRequest);

    /// <summary>
    /// The second namespace's catalog identity is preserved.
    /// </summary>
    [Fact] void should_recover_the_second_namespace_record() => _otherResult.Single().Record.ShouldEqual(_otherReserved);
}
