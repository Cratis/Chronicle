// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>An operation cannot take over a fixed grant belonging to a different attempt.</summary>
public class when_the_grant_is_held_by_another_attempt : given.a_generation_build_owner
{
    /// <summary>The fixed grant already admitted for another attempt.</summary>
    GenerationWriterGrant _otherGrant;

    /// <summary>Reserves the candidate but admits a different attempt first.</summary>
    async Task Establish()
    {
        var record = (await Reserve()).Record!;
        var binding = new GenerationObserverBinding(_key, record.Identity, _request.Execution.EventSequence, _request.Execution.Revision);
        _otherGrant = (await _catalog.TryAcquire(Store, binding, new(Guid.NewGuid()), expectedEpoch: 0))!;
    }

    /// <summary>Attempts admission using the original operation identity.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The owner fails closed instead of minting a takeover epoch.</summary>
    [Fact] void should_refuse_the_grant() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.GrantRefused);

    /// <summary>Another attempt's authority is never returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();

    /// <summary>The existing writer's fixed grant remains unchanged.</summary>
    [Fact] async Task should_preserve_the_existing_grant() => (await _catalog.GetAdmitted(Store, _otherGrant.Binding)).ShouldEqual(_otherGrant);

    /// <summary>No replacement candidate is allocated.</summary>
    [Fact] async Task should_keep_one_candidate() => (await Binding()).Generations.Count.ShouldEqual(2);
}
