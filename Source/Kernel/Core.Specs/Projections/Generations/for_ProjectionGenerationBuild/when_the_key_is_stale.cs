// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A request from an old incarnation cannot reserve in the current store.</summary>
public class when_the_key_is_stale : given.a_generation_build_owner
{
    /// <summary>Resolves a different store incarnation.</summary>
    void Establish() => _keys.For(Store, _key.Namespace, _key.Projection).Returns(_key with { Incarnation = new StoreIncarnation(Guid.NewGuid()) });

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The stale identity is refused.</summary>
    [Fact] void should_refuse_the_stale_key() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.StaleKey);

    /// <summary>No candidate is reserved.</summary>
    [Fact] async Task should_leave_only_the_serving_generation() => (await Binding()).Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
